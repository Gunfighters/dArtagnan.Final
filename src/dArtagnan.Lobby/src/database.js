import mysql from 'mysql2/promise';
import { config } from './config.js';
import { logger } from './logger.js';
import { constants } from './constants.js';

// DB 연결 풀
let pool;

// 쿼리 실행 헬퍼 (pool에서 자동으로 연결 가져오기)
async function getConnection() {
    return pool;
}

// DB 연결 테스트 및 데이터베이스 자동 생성
async function testConnection() {
    try {
        // 1. 데이터베이스 없이 MySQL 연결
        const rootConnection = await mysql.createConnection({
            host: config.database.host,
            user: config.database.user,
            password: config.database.password,
        });

        // 2. 데이터베이스 존재 확인 및 생성
        await rootConnection.execute(`
            CREATE DATABASE IF NOT EXISTS ${config.database.name}
            CHARACTER SET utf8mb4
            COLLATE utf8mb4_unicode_ci
        `);
        await rootConnection.end();

        // 3. Connection Pool 생성 (자동 재연결 지원)
        pool = mysql.createPool({
            host: config.database.host,
            user: config.database.user,
            password: config.database.password,
            database: config.database.name,
            timezone: '+09:00', // 한국 시간대 (KST)
            waitForConnections: true,
            connectionLimit: 10,
            queueLimit: 0,
            enableKeepAlive: true,
            keepAliveInitialDelay: 0
        });

        // 연결 테스트
        const connection = await pool.getConnection();
        await connection.execute('SELECT 1');
        connection.release();

        logger.info('[DB] MySQL Connection Pool 생성 완료');
    } catch (error) {
        logger.error(`[DB] MySQL 연결 실패: ${error.message}`);
        process.exit(1);
    }
}

// 유저 테이블 생성
async function createTables() {
    const createUserTable = `
        CREATE TABLE IF NOT EXISTS users (
            id INT PRIMARY KEY AUTO_INCREMENT,
            provider VARCHAR(10) NOT NULL,
            provider_id VARCHAR(255) NOT NULL,
            nickname VARCHAR(50),
            gold INT DEFAULT 0,
            silver INT DEFAULT 0,
            crystal INT DEFAULT 0,
            needs_nickname TINYINT(1) DEFAULT 1,
            win_count INT DEFAULT 0,
            experience INT DEFAULT 0,
            last_login_at TIMESTAMP NULL,
            last_logout_at TIMESTAMP NULL,
            last_daily_reward_at TIMESTAMP NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

            UNIQUE KEY unique_provider (provider, provider_id),
            INDEX idx_nickname (nickname)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `;

    const createInventoryTable = `
        CREATE TABLE IF NOT EXISTS inventory (
            user_id INT,
            part_type VARCHAR(50) NOT NULL,
            costume_id VARCHAR(100) NOT NULL,
            acquired_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (user_id, part_type, costume_id),
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
            INDEX idx_user_part (user_id, part_type)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `;

    const createEquippedCostumesTable = `
        CREATE TABLE IF NOT EXISTS equipped_costumes (
            user_id INT PRIMARY KEY,
            helmet VARCHAR(100) NOT NULL,
            armor VARCHAR(100) NOT NULL,
            firearm1h VARCHAR(100) NOT NULL,
            body VARCHAR(100) NOT NULL,
            body_paint VARCHAR(8) NOT NULL,
            hair VARCHAR(100) NOT NULL,
            hair_paint VARCHAR(8) NOT NULL,
            beard VARCHAR(100) NOT NULL,
            beard_paint VARCHAR(8) NOT NULL,
            eyebrows VARCHAR(100) NOT NULL,
            eyes VARCHAR(100) NOT NULL,
            eyes_paint VARCHAR(8) NOT NULL,
            ears VARCHAR(100) NOT NULL,
            mouth VARCHAR(100) NOT NULL,
            makeup VARCHAR(100) NOT NULL,
            mask VARCHAR(100) NOT NULL,
            earrings VARCHAR(100) NOT NULL,
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `;

    try {
        await pool.execute(createUserTable);
        await pool.execute(createInventoryTable);
        await pool.execute(createEquippedCostumesTable);
    } catch (error) {
        logger.error(`[DB] 테이블 생성 실패: ${error.message}`);
    }
}

/**
 * 사용자 조회
 */
export async function findUserByProvider(providerId) {
    try {
        const [rows] = await pool.execute(
            'SELECT * FROM users WHERE provider_id = ?',
            [providerId]
        );
        return rows[0] || null;
    } catch (error) {
        logger.error('[DB] 사용자 조회 실패:', error);
        return null;
    }
}

/**
 * 사용자 생성 (기본 코스튬 자동 지급)
 */
export async function createUser(provider, providerId, nickname) {
    const connection = await pool.getConnection();
    try {
        // 트랜잭션 시작
        await connection.beginTransaction();

        // 사용자 생성 (기본 골드 지급)
        const [result] = await connection.execute(
            'INSERT INTO users (provider, provider_id, nickname, gold) VALUES (?, ?, ?, 800)',
            [provider, providerId, nickname]
        );
        const userId = result.insertId;

        // 기본 머리 코스튬 랜덤 선택
        const defaultHairOptions = ['Common.Basic.Hair.Default', 'Common.Basic.Hair.Type15 [HideEars]'];
        const randomHair = defaultHairOptions[Math.floor(Math.random() * defaultHairOptions.length)];

        // 착용 가능한 파츠 중 기본 코스튬이 아닌 것만 인벤토리에 저장
        // (기본 코스튬은 getUserInventory에서 자동으로 병합됨)
        const initialParts = [
            { partType: 'Helmet', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Helmet },
            { partType: 'Armor', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Armor },
            { partType: 'Firearm1H', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Firearm1H },
            { partType: 'Hair', costumeId: randomHair },
            { partType: 'Eyebrows', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Eyebrows },
            { partType: 'Eyes', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Eyes },
            { partType: 'Ears', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Ears },
            { partType: 'Mouth', costumeId: constants.EQUIPPABLE_SLOTS_DEFAULTS.Mouth }
        ];

        for (const part of initialParts) {
            // 기본 코스튬이 아닌 것만 DB에 저장
            if (!isDefaultCostume(part.partType, part.costumeId)) {
                await connection.execute(
                    'INSERT INTO inventory (user_id, part_type, costume_id) VALUES (?, ?, ?)',
                    [userId, part.partType, part.costumeId]
                );
            }
        }

        // 착용 코스튬 초기화 (constants에서 가져옴)
        await connection.execute(
            `INSERT INTO equipped_costumes (
                user_id, helmet, armor, firearm1h,
                body, body_paint, hair, hair_paint, beard, beard_paint,
                eyebrows, eyes, eyes_paint, ears, mouth, makeup, mask, earrings
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
            [
                userId,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Helmet,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Armor,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Firearm1H,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Body,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Body_Paint,
                randomHair,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Hair_Paint,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Beard,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Beard_Paint,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Eyebrows,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Eyes,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Eyes_Paint,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Ears,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Mouth,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Makeup,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Mask,
                constants.EQUIPPABLE_SLOTS_DEFAULTS.Earrings
            ]
        );

        // 트랜잭션 커밋
        await connection.commit();
        return userId;
    } catch (error) {
        // 트랜잭션 롤백
        await connection.rollback();
        logger.error('[DB] 사용자 생성 실패:', error);
        return null;
    } finally {
        connection.release();
    }
}

/**
 * 닉네임 중복 체크
 */
export async function checkNicknameDuplicate(nickname) {
    try {
        const [rows] = await pool.execute(
            'SELECT id FROM users WHERE nickname = ?',
            [nickname]
        );
        return rows.length > 0;
    } catch (error) {
        logger.error('[DB] 닉네임 중복 체크 실패:', error);
        return true; // 에러시 중복으로 처리
    }
}

/**
 * 닉네임 설정
 */
export async function setUserNickname(provider, providerId, nickname) {
    try {
        await pool.execute(
            'UPDATE users SET nickname = ?, needs_nickname = 0 WHERE provider = ? AND provider_id = ?',
            [nickname, provider, providerId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 닉네임 설정 실패:', error);
        return false;
    }
}

/**
 * 임시 닉네임 생성
 */
export function generateTempNickname() {
    const timestamp = Date.now().toString(36);
    return `User${timestamp}`;
}

/**
 * 금화 업데이트
 */
export async function updateUserGold(userId, gold) {
    try {
        await pool.execute(
            'UPDATE users SET gold = ? WHERE id = ?',
            [gold, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 금화 업데이트 실패:', error);
        return false;
    }
}

/**
 * 실버 업데이트
 */
export async function updateUserSilver(userId, silver) {
    try {
        await pool.execute(
            'UPDATE users SET silver = ? WHERE id = ?',
            [silver, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 실버 업데이트 실패:', error);
        return false;
    }
}

/**
 * 실버 추가
 */
export async function addUserSilver(userId, amount) {
    try {
        await pool.execute(
            'UPDATE users SET silver = silver + ? WHERE id = ?',
            [amount, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 실버 추가 실패:', error);
        return false;
    }
}

/**
 * 크리스탈 업데이트
 */
export async function updateUserCrystal(userId, crystal) {
    try {
        await pool.execute(
            'UPDATE users SET crystal = ? WHERE id = ?',
            [crystal, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 크리스탈 업데이트 실패:', error);
        return false;
    }
}

/**
 * 크리스탈 추가
 */
export async function addUserCrystal(userId, amount) {
    try {
        await pool.execute(
            'UPDATE users SET crystal = crystal + ? WHERE id = ?',
            [amount, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 크리스탈 추가 실패:', error);
        return false;
    }
}

/**
 * 착용 코스튬 조회
 */
export async function getEquippedCostumes(userId) {
    try {
        const [rows] = await pool.execute(
            'SELECT * FROM equipped_costumes WHERE user_id = ?',
            [userId]
        );

        if (rows.length === 0) {
            logger.error(`[DB] equipped_costumes에 user_id=${userId} 데이터가 없음`);
            return constants.EQUIPPABLE_SLOTS_DEFAULTS;
        }

        const row = rows[0];
        return {
            Helmet: row.helmet,
            Armor: row.armor,
            Firearm1H: row.firearm1h,
            Body: row.body,
            Body_Paint: row.body_paint,
            Hair: row.hair,
            Hair_Paint: row.hair_paint,
            Beard: row.beard,
            Beard_Paint: row.beard_paint,
            Eyebrows: row.eyebrows,
            Eyes: row.eyes,
            Eyes_Paint: row.eyes_paint,
            Ears: row.ears,
            Mouth: row.mouth,
            Makeup: row.makeup,
            Mask: row.mask,
            Earrings: row.earrings
        };
    } catch (error) {
        logger.error('[DB] 착용 코스튬 조회 실패:', error);
        return constants.EQUIPPABLE_SLOTS_DEFAULTS;
    }
}

/**
 * 착용 코스튬 변경 (파츠별)
 */
export async function updateEquippedCostume(userId, partType, costumeId) {
    try {
        const columnMap = {
            'Helmet': 'helmet',
            'Armor': 'armor',
            'Firearm1H': 'firearm1h',
            'Body': 'body',
            'Body_Paint': 'body_paint',
            'Hair': 'hair',
            'Hair_Paint': 'hair_paint',
            'Beard': 'beard',
            'Beard_Paint': 'beard_paint',
            'Eyebrows': 'eyebrows',
            'Eyes': 'eyes',
            'Eyes_Paint': 'eyes_paint',
            'Ears': 'ears',
            'Mouth': 'mouth',
            'Makeup': 'makeup',
            'Mask': 'mask',
            'Earrings': 'earrings'
        };

        const column = columnMap[partType];
        if (!column) {
            logger.error(`[DB] 잘못된 파츠 타입: ${partType}`);
            return false;
        }

        await pool.execute(
            `UPDATE equipped_costumes SET ${column} = ? WHERE user_id = ?`,
            [costumeId, userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 착용 코스튬 변경 실패:', error);
        return false;
    }
}

/**
 * 기본 코스튬인지 확인하는 헬퍼 함수
 */
export function isDefaultCostume(partType, costumeId) {
    const defaults = constants.DEFAULT_OWNED_COSTUMES[partType];
    return defaults && defaults.includes(costumeId);
}

/**
 * 인벤토리에 코스튬 추가 (기본 코스튬은 DB에 저장하지 않음)
 */
export async function addCostumeToInventory(userId, partType, costumeId) {
    try {
        // 기본 코스튬은 DB에 저장하지 않음
        if (isDefaultCostume(partType, costumeId)) {
            return true;
        }

        await pool.execute(
            'INSERT IGNORE INTO inventory (user_id, part_type, costume_id) VALUES (?, ?, ?)',
            [userId, partType, costumeId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 코스튬 추가 실패:', error);
        return false;
    }
}

/**
 * 사용자 인벤토리 조회 (파츠타입별 그룹화)
 * DB에 저장된 코스튬 + 기본 코스튬 자동 병합
 * @returns {Object} { Helmet: ["Basic.Iron", ...], Vest: [...], Paint: ["#FF0000", ...] }
 */
export async function getUserInventory(userId) {
    try {
        const [rows] = await pool.execute(
            'SELECT part_type, costume_id FROM inventory WHERE user_id = ? ORDER BY part_type, costume_id',
            [userId]
        );

        // 파츠타입별 그룹화 (DB 데이터)
        const grouped = {};
        for (const row of rows) {
            if (!grouped[row.part_type]) {
                grouped[row.part_type] = [];
            }
            grouped[row.part_type].push(row.costume_id);
        }

        // 기본 코스튬 자동 추가 (중복 방지)
        for (const [partType, costumes] of Object.entries(constants.DEFAULT_OWNED_COSTUMES)) {
            if (!grouped[partType]) {
                grouped[partType] = [];
            }
            for (const costumeId of costumes) {
                if (!grouped[partType].includes(costumeId)) {
                    grouped[partType].push(costumeId);
                }
            }
        }

        return grouped;
    } catch (error) {
        logger.error('[DB] 인벤토리 조회 실패:', error);
        return {};
    }
}

/**
 * 마지막 로그인 시간 업데이트
 */
export async function updateLastLogin(userId) {
    try {
        await pool.execute(
            'UPDATE users SET last_login_at = NOW() WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 마지막 로그인 업데이트 실패:', error);
        return false;
    }
}

/**
 * 승리 횟수 증가
 */
export async function incrementWinCount(userId) {
    try {
        await pool.execute(
            'UPDATE users SET win_count = win_count + 1 WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 승리 횟수 증가 실패:', error);
        return false;
    }
}

/**
 * 마지막 로그아웃 시간 업데이트
 */
export async function updateLastLogout(userId) {
    try {
        await pool.execute(
            'UPDATE users SET last_logout_at = NOW() WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 마지막 로그아웃 업데이트 실패:', error);
        return false;
    }
}

/**
 * 마지막 일일 보상 수령 시간 업데이트
 */
export async function updateLastDailyReward(userId) {
    try {
        await pool.execute(
            'UPDATE users SET last_daily_reward_at = NOW() WHERE id = ?',
            [userId]
        );
        return true;
    } catch (error) {
        logger.error('[DB] 마지막 일일 보상 업데이트 실패:', error);
        return false;
    }
}

/**
 * 특정 레벨에 도달하기 위한 누적 총 경험치 계산
 * 레벨 1→2: 100, 2→3: 200, 3→4: 300...
 * 레벨 n까지의 누적 경험치 = n * (n-1) * 100 / 2
 * @param {number} level - 레벨
 * @returns {number} 해당 레벨에 도달하기 위한 누적 총 경험치
 */
function getTotalExpForLevel(level) {
    // 레벨 1은 0 경험치부터 시작
    if (level <= 1) return 0;
    return (level - 1) * level * constants.EXP_PER_LEVEL / 2;
}

/**
 * 총 경험치로부터 레벨과 현재 경험치 계산
 * @param {number} totalExp - 총 경험치
 * @returns {{level: number, currentExp: number, expToNextLevel: number}}
 */
export function calculateLevelAndExp(totalExp) {
    // 레벨 찾기 (이분 탐색 또는 순차 탐색)
    let level = 1;
    while (getTotalExpForLevel(level + 1) <= totalExp) {
        level++;
    }

    // 현재 레벨의 시작 경험치
    const expForCurrentLevel = getTotalExpForLevel(level);

    // 현재 레벨에서의 경험치
    const currentExp = totalExp - expForCurrentLevel;

    // 다음 레벨까지 필요한 경험치 (레벨 * 100)
    const expToNextLevel = level * constants.EXP_PER_LEVEL;

    return { level, currentExp, expToNextLevel };
}

/**
 * 경험치 추가
 * @param {number} userId - 사용자 ID
 * @param {number} expGain - 획득한 경험치
 * @returns {Promise<{newExp: number}>}
 */
export async function addExperience(userId, expGain) {
    try {
        // 1. 현재 경험치 조회
        const [rows] = await pool.execute(
            'SELECT experience FROM users WHERE id = ?',
            [userId]
        );

        if (rows.length === 0) {
            logger.error(`[DB] 사용자를 찾을 수 없음: userId=${userId}`);
            return null;
        }

        const currentExp = rows[0].experience;
        const newExp = currentExp + expGain;

        // 2. DB 업데이트
        await pool.execute(
            'UPDATE users SET experience = ? WHERE id = ?',
            [newExp, userId]
        );

        logger.info(`[DB] 경험치 업데이트: userId=${userId}, +${expGain}exp (${currentExp}→${newExp})`);

        return { newExp };
    } catch (error) {
        logger.error('[DB] 경험치 업데이트 실패:', error);
        return null;
    }
}

/**
 * DB 초기화
 */
export async function initDatabase() {
    await testConnection();
    await createTables();
}
