/**
 * 로비 서버 상수 정의
 */
export const constants = {
    // 경험치 관련 상수
    EXP_PER_LEVEL: 100,

    // 순위별 경험치 (고정)
    RANK_EXPERIENCE: {
        1: 100,
        2: 80,
        3: 60,
        4: 40,
        5: 20,
        6: 10,
        7: 10,
        8: 0
    },

    // 일일 보상 관련 상수
    DAILY_REWARD_GOLD: 200,
    DAILY_REWARD_RESET_HOUR: 0, // 일일 보상 초기화 기준 시간 (0 = 자정)

    // 인앱 결제 상품 (productId: crystal_1 ~ crystal_5)
    IAP_PRODUCTS: {
        'com.gunfighters.dartagnan.crystal_1': { crystalAmount: 100 },   //1100원
        'com.gunfighters.dartagnan.crystal_2': { crystalAmount: 500 },   //5500원
        'com.gunfighters.dartagnan.crystal_3': { crystalAmount: 1200 },  //11000원  200보석이득
        'com.gunfighters.dartagnan.crystal_4': { crystalAmount: 4000 },  //33000원  1000보석이득
    },

    // 중복 획득 시 등급별 실버 반환 배율 (금화 룰렛 비용에 곱할 값)
    DUP_TIER_SILVER_MULTIPLIERS: {
        COMMON: 1,
        RARE: 3,
        EPIC: 6,
        LEGENDARY: 10
    },

    // 등급별 실버 직접 구매 배율 (룰렛 금화 비용에 곱할 값)
    TIER_DIRECT_SILVER_PRICE_MULTIPLIER: {
        COMMON: 3,    // 실제 가격 = 룰렛 금화 비용 × 3
        RARE: 9,      // 실제 가격 = 룰렛 금화 비용 × 9
        EPIC: 18,     // 실제 가격 = 룰렛 금화 비용 × 18
        LEGENDARY: 30 // 실제 가격 = 룰렛 금화 비용 × 30
    },

    // 등급별 크리스탈 직접 구매 배율 (룰렛 크리스탈 비용에 곱할 값)
    TIER_DIRECT_CRYSTAL_PRICE_MULTIPLIER: {
        COMMON: 1,    // 실제 가격 = 룰렛 크리스탈 비용 × 1
        RARE: 3,      // 실제 가격 = 룰렛 크리스탈 비용 × 3
        EPIC: 6,      // 실제 가격 = 룰렛 크리스탈 비용 × 6
        LEGENDARY: 10 // 실제 가격 = 룰렛 크리스탈 비용 × 10
    },

    // 공통 등급별 확률 (모든 부위에 공통 적용)
    TIER_WEIGHTS: {
        COMMON: 0.55,
        RARE: 0.31,
        EPIC: 0.13,
        LEGENDARY: 0.03
    },

    // 모든 유저가 기본으로 보유한 코스튬 (DB에 저장하지 않고 자동 제공)
    DEFAULT_OWNED_COSTUMES: {
        Body: [
            'Common.Basic.Body.Type4',  // 기본 착용
            'Common.Basic.Body.Human',
            'Common.Basic.Body.Type1',
            'Common.Basic.Body.Type2',
            'Common.Basic.Body.Type3',
            'Common.Basic.Body.Type5',
            'Common.Basic.Body.Type6'
        ],
        Hair: [
            'Common.Basic.Hair.Default',
            'Common.Basic.Hair.Type15 [HideEars]',
        ],
        Helmet: ['None'],
        Beard: ['None'],
        Makeup: ['None'],
        Mask: ['None'],
        Earrings: ['None'],
        Paint: ['FFC878FF', '963200FF', '00C8FFFF']  // 피부색, 머리색, 눈색
    },

    // key = 착용 가능한 부위 , value = 기본 코스튬
    EQUIPPABLE_SLOTS_DEFAULTS: {
        Helmet: 'None',
        Armor: 'MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]',
        Firearm1H: 'MilitaryHeroes.Basic.Firearm1H.DesertEagle',
        Body: 'Common.Basic.Body.Type4',
        Body_Paint: 'FFC878FF',      // 기본 피부색 (밝은 살색)
        Hair: 'Common.Basic.Hair.Default',
        Hair_Paint: '963200FF',      // 기본 머리색 (갈색)
        Beard: 'None',               // 기본: 안 입음
        Beard_Paint: '963200FF',     // 기본 수염색 (갈색)
        Eyebrows: 'Common.Basic.Eyebrows.Eyebrows14',
        Eyes: 'Common.Basic.Eyes.Boy',
        Eyes_Paint: '00C8FFFF',      // 기본 눈 색 (하늘색)
        Ears: 'Common.Basic.Ears.Human',
        Mouth: 'Common.Basic.Mouth.Default',
        Makeup: 'None',              // 기본: 안 입음
        Mask: 'None',                // 기본: 안 입음
        Earrings: 'None'             // 기본: 안 입음
    },

    // 파츠별 상점 설정
    SHOPS: {
        Helmet: {
            ROULETTE_GOLD_COST: 200,
            ROULETTE_CRYSTAL_COST: 20,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'MilitaryHeroes.Cyberpunk.Armor.BulletproofVest [ShowEars]',
                    'Common.Casual.Armor.CasualBoy1 [ShowEars]',
                    'Common.Casual.Armor.CasualBoy2 [ShowEars]',
                    'Common.Casual.Armor.CasualBoy3 [ShowEars]',
                    'MilitaryHeroes.Cyberpunk.Armor.LeatherJacket [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.HunterOutfit[ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Bandit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Cowboy [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Mobster [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.WorkerOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.RebelTypeA',
                    'MilitaryHeroes.Basic.Armor.RebelTypeB',
                    'MilitaryHeroes.Basic.Armor.SoldierOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.TankmanTypeA',
                ],
                RARE: [
                    'MilitaryHeroes.Cyberpunk.Armor.SilentEngineer',
                    'MilitaryHeroes.Basic.Armor.Swat',
                    'MilitaryHeroes.Cyberpunk.Armor.HazardArmor',
                    'MilitaryHeroes.Cyberpunk.Armor.MobileTrooper',
                    'MilitaryHeroes.Cyberpunk.Armor.WarfareArmor',
                    'MilitaryHeroes.Basic.Armor.FirefighterOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.TankmanTypeB [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Sailor [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.PoliceOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]',
                ],
                EPIC: [
                    'MilitaryHeroes.Basic.Armor.Pilot',
                    'MilitaryHeroes.Basic.Armor.Astronaut',
                    'MilitaryHeroes.Cyberpunk.Armor.MistBuster',
                    'Common.Christmas.Armor.NutcrackerCostume [ShowEars]',
                    'Common.Christmas.Armor.SantaCostume [ShowEars]',
                    'Common.Christmas.Armor.SantaHelperCostume [ShowEars]',
                ],
                LEGENDARY: [
                    'Common.Christmas.Armor.SantaDeerCostume [ShowEars] [FullHair]',
                    'Common.Casual.Armor.CasualGirl2 [ShowEars] [FullHair]',
                    'Common.Casual.Armor.CasualGirl3 [ShowEars] [FullHair]',
                    'Common.Casual.Armor.CasualGirl1 [ShowEars] [FullHair]',
                ]
            }
        },
        Armor: {
            ROULETTE_GOLD_COST: 150,
            ROULETTE_CRYSTAL_COST: 15,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'MilitaryHeroes.Cyberpunk.Armor.BulletproofVest [ShowEars]',
                    'Common.Casual.Armor.CasualBoy1 [ShowEars]',
                    'Common.Casual.Armor.CasualBoy2 [ShowEars]',
                    'Common.Casual.Armor.CasualBoy3 [ShowEars]',
                    'MilitaryHeroes.Cyberpunk.Armor.LeatherJacket [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.HunterOutfit[ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Bandit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Cowboy [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Mobster [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.WorkerOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.RebelTypeA',
                    'MilitaryHeroes.Basic.Armor.RebelTypeB',
                    'MilitaryHeroes.Basic.Armor.SoldierOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.TankmanTypeA',
                ],
                RARE: [
                    'MilitaryHeroes.Cyberpunk.Armor.SilentEngineer',
                    'MilitaryHeroes.Basic.Armor.Swat',
                    'MilitaryHeroes.Cyberpunk.Armor.HazardArmor',
                    'MilitaryHeroes.Cyberpunk.Armor.MobileTrooper',
                    'MilitaryHeroes.Basic.Armor.Pilot',
                    'MilitaryHeroes.Basic.Armor.PoliceOutfit [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.Sailor [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.FirefighterOutfit [ShowEars]',
                ],
                EPIC: [
                    'MilitaryHeroes.Basic.Armor.Astronaut',
                    'MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]',
                    'MilitaryHeroes.Basic.Armor.TankmanTypeB [ShowEars]',
                    'MilitaryHeroes.Cyberpunk.Armor.MistBuster',
                    'MilitaryHeroes.Cyberpunk.Armor.WarfareArmor',
                    'Common.Christmas.Armor.NutcrackerCostume [ShowEars]',
                    'Common.Christmas.Armor.SantaCostume [ShowEars]',
                    'Common.Christmas.Armor.SantaHelperCostume [ShowEars]',
                ],
                LEGENDARY: [
                    'Common.Christmas.Armor.SantaDeerCostume [ShowEars] [FullHair]',
                    'Common.Casual.Armor.CasualGirl2 [ShowEars] [FullHair]',
                    'Common.Casual.Armor.CasualGirl3 [ShowEars] [FullHair]',
                    'Common.Casual.Armor.CasualGirl1 [ShowEars] [FullHair]',
                ]
            }
        },
        Firearm1H: {
            ROULETTE_GOLD_COST: 120,
            ROULETTE_CRYSTAL_COST: 12,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'MilitaryHeroes.Basic.Firearm1H.DesertEagle',
                    'MilitaryHeroes.Basic.Firearm1H.USP',
                    'MilitaryHeroes.Basic.Firearm1H.SP81',
                    'MilitaryHeroes.Basic.Firearm1H.UziPro',
                    'MilitaryHeroes.Basic.Firearm1H.Beretta',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.ZvezdaPM2',
                    'MilitaryHeroes.Basic.Firearm1H.MicroUzi',
                    'MilitaryHeroes.Basic.Firearm1H.Cobra',
                    'MilitaryHeroes.Basic.Firearm1H.ColtM1911',
                    'MilitaryHeroes.Basic.Firearm1H.Glock18',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.Lifetaker',
                    'MilitaryHeroes.Basic.Firearm1H.LugerP08',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.ShokerGun',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.ShikaroType155',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.NanotechPolice',
                ],
                RARE: [
                    'MilitaryHeroes.Basic.Firearm1H.Anaconda',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.Drifter',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.NanotechRevolver',
                    'MilitaryHeroes.Basic.Firearm1H.MAC10',
                    'MilitaryHeroes.Basic.Firearm1H.MauserC96',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.RitterPistol',
                    'MilitaryHeroes.Basic.Firearm1H.Scorpion',
                    'MilitaryHeroes.Basic.Firearm1H.TEC9',
                    'MilitaryHeroes.Basic.Firearm1H.Vector',
                ],
                EPIC: [
                    'Common.Christmas.Firearm1H.Musket',
                    'MilitaryHeroes.Basic.Firearm1H.D2',
                    'MilitaryHeroes.Basic.Firearm1H.Peacemaker',
                    'MilitaryHeroes.Basic.Firearm1H.USPS',
                    'MilitaryHeroes.Basic.Firearm1H.VectorS',
                    'MilitaryHeroes.Cyberpunk.Firearm1H.ZvezdaSMG',
                ],
                LEGENDARY: [
                    'Common.Christmas.Firearm1H.CandyCannon',
                ]
            }
        },
        //Body는 현재 기획상 모든 파츠가 DEFAULT_OWNED_COSTUMES에 포함되어 있으므로 판매 대상이 아님
        Body: {
            ROULETTE_GOLD_COST: 10,
            ROULETTE_CRYSTAL_COST: 1,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Body.Type4',
                    'Common.Basic.Body.Human',
                    'Common.Basic.Body.Type1',
                    'Common.Basic.Body.Type2',
                    'Common.Basic.Body.Type3',
                    'Common.Basic.Body.Type5',
                    'Common.Basic.Body.Type6'
                ],
                RARE: [
                ],
                EPIC: [
                ],
                LEGENDARY: [
                ]
            }
        },
        Hair: {
            ROULETTE_GOLD_COST: 200,
            ROULETTE_CRYSTAL_COST: 20,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Hair.Default',
                    'Common.Basic.Hair.Type15 [HideEars]',
                    'Common.Basic.Hair.BuzzCut',
                    'Common.Basic.Hair.Type1',
                    'Common.Basic.Hair.Type2',
                    'Common.Basic.Hair.Type7',
                    'Common.Basic.Hair.Type8',
                    'Common.Basic.Hair.Type9',
                    'Common.Basic.Hair.Type3',
                    'Common.Basic.Hair.Type4',
                    'Common.Basic.Hair.Type5',
                    'Common.Basic.Hair.Type6',
                    'Common.Basic.Hair.Mohawk',
                    'Common.Basic.Hair.CasualMessy',
                    'Common.Basic.Hair.CollegeBoy',
                    'Common.Basic.Hair.FrenchCrop',
                    'Common.Basic.Hair.MessyMedium',
                    'Common.Basic.Hair.NeatSidePart',
                    'Common.Basic.Hair.Shaggy',
                    'Common.Basic.Hair.ShortHair',
                    'Common.Basic.Hair.SideBangs',
                    'Common.Basic.Hair.SlickBack',
                    'Common.Basic.Hair.Spiky',
                    'Common.Basic.Hair.SpikyFringeUp',
                    'Common.Basic.Hair.Type19 [HideEars]',
                ],
                RARE: [
                    'Common.Basic.Hair.PopStyle',
                    'Common.Basic.Hair.FringeUp',
                    'Common.Basic.Hair.BroFlow',
                    'Common.Basic.Hair.Type18',
                    'Common.Basic.Hair.Type10 [HideEars]',
                    'Common.Basic.Hair.Type11 [HideEars]',
                    'Common.Basic.Hair.Type13 [HideEars]',
                    'Common.Basic.Hair.Type14 [HideEars]',
                    'Common.Basic.Hair.Type16 [HideEars]',
                ],
                EPIC: [
                    'Common.Basic.Hair.Type12',
                    'Common.Basic.Hair.Pigtail',
                    'Common.Basic.Hair.ShortTail',
                    'Common.Basic.Hair.SpikyLayered',
                    'Common.Basic.Hair.Twintales',
                    'Common.Basic.Hair.Type20',
                    'Common.Basic.Hair.Type21',
                    'Common.Basic.Hair.Type17',
                ],
                LEGENDARY: [
                    'Common.Gradient [NoPaint].Hair.SideBangsBlue',
                    'Common.Gradient [NoPaint].Hair.SideBangsPink',
                    'Common.Gradient [NoPaint].Hair.SideBangsSunny',
                    'Common.Gradient [NoPaint].Hair.TwintalesBlue',
                    'Common.Gradient [NoPaint].Hair.TwintalesPink',
                    'Common.Gradient [NoPaint].Hair.TwintalesSunny',
                    'Common.Gradient [NoPaint].Hair.SlickBackBlue',
                    'Common.Gradient [NoPaint].Hair.SlickBackPink',
                    'Common.Gradient [NoPaint].Hair.SlickBackSunny',
                    'Common.Gradient [NoPaint].Hair.ShortTailBlue',
                    'Common.Gradient [NoPaint].Hair.ShortTailPink',
                    'Common.Gradient [NoPaint].Hair.ShortTailSunny',
                    'Common.Gradient [NoPaint].Hair.PigtailBlue',
                    'Common.Gradient [NoPaint].Hair.PigtailPink',
                    'Common.Gradient [NoPaint].Hair.PigtailSunny',
                    'Common.Gradient [NoPaint].Hair.FringeUpBlue',
                    'Common.Gradient [NoPaint].Hair.FringeUpPink',
                    'Common.Gradient [NoPaint].Hair.FringeUpSunny',
                    'Common.Gradient [NoPaint].Hair.BroFlowBlue',
                    'Common.Gradient [NoPaint].Hair.BroFlowPink',
                    'Common.Gradient [NoPaint].Hair.BroFlowSunny',
                ],
            }
        },
        Beard: {
            ROULETTE_GOLD_COST: 100,
            ROULETTE_CRYSTAL_COST: 10,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Beard.Type4',
                    'Common.Basic.Beard.Type5',
                    'Common.Basic.Beard.Type11',
                    'Common.Basic.Beard.Type7',
                    'Common.Basic.Beard.Type9',
                ],
                RARE: [
                    'Common.Basic.Beard.Type1',
                    'Common.Basic.Beard.Type3',
                    'Common.Basic.Beard.Type2',
                    'Common.Basic.Beard.Type8',
                ],
                EPIC: [
                    'Common.Basic.Beard.Type10',
                    'Common.Basic.Beard.Type6',
                ],
                LEGENDARY: [
                    'Common.Basic.Beard.Type12',
                ]
            }
        }, //Eyebrows는 현재 기획상 판매하지 않고 있음 (클라측에서 상점목록 하드코딩하고 있는데 eyeBrows는 빼고있음)
        Eyebrows: {
            ROULETTE_GOLD_COST: 100,
            ROULETTE_CRYSTAL_COST: 10,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Eyebrows.Eyebrows14',
                    'Common.Emoji.Eyebrows.AngryEyebrows',
                    'Common.Emoji.Eyebrows.DeadEyebrows1',
                    'Common.Emoji.Eyebrows.DeadEyebrows2',
                    'Common.Basic.Eyebrows.Default',
                    'Common.Basic.Eyebrows.Eyebrows1',
                    'Common.Basic.Eyebrows.Eyebrows10',
                    'Common.Basic.Eyebrows.Eyebrows11',
                    'Common.Basic.Eyebrows.Eyebrows12',
                    'Common.Basic.Eyebrows.Eyebrows13',
                    'Common.Basic.Eyebrows.Eyebrows15',
                    'Common.Basic.Eyebrows.Eyebrows16',
                    'Common.Basic.Eyebrows.Eyebrows17',
                    'Common.Basic.Eyebrows.Eyebrows18'
                ],
                RARE: [
                    'Common.Basic.Eyebrows.Eyebrows19',
                    'Common.Basic.Eyebrows.Eyebrows2',
                    'Common.Basic.Eyebrows.Eyebrows20',
                    'Common.Basic.Eyebrows.Eyebrows21',
                    'Common.Basic.Eyebrows.Eyebrows22',
                    'Common.Basic.Eyebrows.Eyebrows3',
                    'Common.Basic.Eyebrows.Eyebrows4',
                    'Common.Basic.Eyebrows.Eyebrows5'
                ],
                EPIC: [
                    'Common.Basic.Eyebrows.Eyebrows6',
                    'Common.Basic.Eyebrows.Eyebrows7',
                    'Common.Basic.Eyebrows.Eyebrows8',
                    'Common.Basic.Eyebrows.Eyebrows9'
                ],
                LEGENDARY: [
                    'Common.Emoji.Eyebrows.QuestionableEyebrows',
                    'Common.Emoji.Eyebrows.SadEyebrows'
                ]
            }
        },
        Eyes: {
            ROULETTE_GOLD_COST: 100,
            ROULETTE_CRYSTAL_COST: 10,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Eyes.Boy',
                    'Common.Basic.Eyes.Asian',
                    'Common.Basic.Eyes.Boy01',
                    'Common.Basic.Eyes.Boy02',
                    'Common.Basic.Eyes.Evil',
                    'Common.Basic.Eyes.Girl',
                    'Common.Basic.Eyes.Girl01',
                    'Common.Basic.Eyes.Girl02',
                    'Common.Basic.Eyes.Girl03',
                    'Common.Basic.Eyes.Girl04',
                    'Common.Basic.Eyes.Girl05',
                    'Common.Basic.Eyes.HardEdge',
                    'Common.Basic.Eyes.Man',
                    'Common.Basic.Eyes.Type11',
                    'Common.Basic.Eyes.Type12',
                    'Common.Basic.Eyes.Type13',
                    'Common.Basic.Eyes.Type14',
                    'Common.Reference.Eyes.ReferenceEyesAsymmetric',
                    'Common.Basic.Eyes.Type01',
                    'Common.Basic.Eyes.Type02',
                    'Common.Basic.Eyes.Type03',
                    'Common.Basic.Eyes.Type04',
                    'Common.Basic.Eyes.Type05',
                    'Common.Basic.Eyes.Type06',
                    'Common.Basic.Eyes.Type07',
                    'Common.Basic.Eyes.Type08',
                    'Common.Basic.Eyes.Type09',
                    'Common.Basic.Eyes.Type10'
                ],
                RARE: [
                    'Common.Basic.Eyes.Saitama',
                    'Common.Emoji.Eyes.SurprsedEyes',
                    'Common.Basic.Eyes.Type16',
                    'Common.Basic.Eyes.Type17',
                    'Common.Basic.Eyes.Type15',
                    'Common.Emoji.Eyes.HappyEyes',
                    'Common.Emoji.Eyes.AngryEyes3',
                    'Common.Emoji.Eyes.DeadEyes1',
                    'Common.Emoji.Eyes.DeadEyes2',
                    'Common.Emoji.Eyes.DeadEyes5',
                    'Common.Emoji.Eyes.DotEyes',
                ],
                EPIC: [
                    'Common.Emoji.Eyes.ScaredEyes',
                    'Common.Emoji.Eyes.DeadEyes3',
                    'Common.Emoji.Eyes.DeadEyes4',
                    'Common.Emoji.Eyes.AngryEyes1',
                    'Common.Emoji.Eyes.AngryEyes2',
                    'Common.Emoji.Eyes.HeartEyes',
                ],
                LEGENDARY: [
                    'Common.Basic.Eyes.Type18',
                    'Common.Basic.Eyes.Type19'
                ]
            }
        },
        Ears: {
            ROULETTE_GOLD_COST: 70,
            ROULETTE_CRYSTAL_COST: 7,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Ears.Human',
                    'Common.Basic.Ears.Elf',
                    'Common.Basic.Ears.Type7',
                    'Common.Reference.Ears.HumanAsymmetric',
                    'Common.Basic.Ears.Type1',
                    'Common.Basic.Ears.Type11',
                    'Common.Basic.Ears.Type2',
                    'Common.Basic.Ears.Type3',
                ],
                RARE: [
                    'Common.Basic.Ears.Type4',
                    'Common.Basic.Ears.Type5',
                    'Common.Basic.Ears.Type6',
                    'Common.Basic.Ears.Type8',
                ],
                EPIC: [
                    'Common.Basic.Ears.Type12',
                    'Common.Basic.Ears.Type10',
                    'Common.Basic.Ears.Type9',
                ],
                LEGENDARY: [
                    'Common.Reference.Ears.Goblin',
                    'Common.Reference.Ears.GoblinAsymmetric',
                ]
            }
        },
        Mouth: {
            ROULETTE_GOLD_COST: 70,
            ROULETTE_CRYSTAL_COST: 7,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Basic.Mouth.Default',
                    'Common.Basic.Mouth.Smile',
                    'Common.Basic.Mouth.Smirk',
                    'Common.Emoji.Mouth.AngryMouth1',
                    'Common.Emoji.Mouth.AngryMouth2',
                    'Common.Emoji.Mouth.AngryMouth3',
                    'Common.Basic.Mouth.CreepySmile',
                    'Common.Emoji.Mouth.DeadMouth1',
                    'Common.Emoji.Mouth.DeadMouth2',
                    'Common.Emoji.Mouth.DeadMouth3',
                    'Common.Basic.Mouth.Dot',
                    'Common.Basic.Mouth.Mouth01',
                    'Common.Basic.Mouth.Mouth05',
                    'Common.Basic.Mouth.Mouth06',
                    'Common.Basic.Mouth.Mouth07',
                    'Common.Basic.Mouth.Mouth08',
                    'Common.Basic.Mouth.Mouth09',
                    'Common.Basic.Mouth.Mouth11',
                    'Common.Basic.Mouth.Mouth14',
                    'Common.Basic.Mouth.Mouth15',
                    'Common.Basic.Mouth.Mouth18'
                ],
                RARE: [
                    'Common.Basic.Mouth.Mouth02',
                    'Common.Basic.Mouth.Mouth12',
                    'Common.Basic.Mouth.Mouth13',
                    'Common.Basic.Mouth.Mouth19',
                    'Common.Basic.Mouth.Mouth20',
                    'Common.Basic.Mouth.Mouth24 [Paint]',
                    'Common.Basic.Mouth.Mouth25 [Paint]',
                    'Common.Basic.Mouth.Mouth26 [Paint]',
                    'Common.Basic.Mouth.Mouth27 [Paint]',
                    'Common.Basic.Mouth.Mouth28 [Paint]'
                ],
                EPIC: [
                    'Common.Basic.Mouth.Mouth03',
                    'Common.Basic.Mouth.Mouth04',
                    'Common.Basic.Mouth.Mouth21',
                    'Common.Basic.Mouth.Mouth22',
                    'Common.Basic.Mouth.Mouth23',
                    'Common.Basic.Mouth.Mouth16',
                    'Common.Basic.Mouth.Mouth17',
                ],
                LEGENDARY: [
                    'Common.Emoji.Mouth.DeadMouth4',
                    'Common.Basic.Mouth.Mouth10',
                ]
            }
        },
        //기획상 다 주고 있음
        Makeup: {
            ROULETTE_GOLD_COST: 100,
            ROULETTE_CRYSTAL_COST: 10,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'MilitaryHeroes.Cyberpunk.Makeup.Type01',
                    'MilitaryHeroes.Cyberpunk.Makeup.Type03',
                    'MilitaryHeroes.Cyberpunk.Makeup.Type02',
                    'MilitaryHeroes.Cyberpunk.Makeup.Type04'
                ],
                RARE: [
                ],
                EPIC: [
                ],
                LEGENDARY: []
            }
        },
        Mask: {
            ROULETTE_GOLD_COST: 120,
            ROULETTE_CRYSTAL_COST: 12,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Common.Mask.GlassesType01',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType01',
                    'Common.Common.Mask.GlassesType02',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType02',
                    'Common.Common.Mask.GlassesType03',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType03',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType04 [Paint]',                    
                    'MilitaryHeroes.Cyberpunk.Mask.LeatherJacketCollar',
                ],
                RARE: [
                    'Common.Christmas.Mask.ElfMask',
                    'MilitaryHeroes.Basic.Mask.BanditMask',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType07',
                    'Common.Common.Mask.MaskType02',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType05',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType10'
                ],
                EPIC: [
                    'Common.Common.Mask.GlassesType05 [Paint]',
                    'Common.Common.Mask.GlassesType04',                    
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType11',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType09',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType06',
                    
                ],
                LEGENDARY: [
                    'Common.Common.Mask.MaskType01',
                    'MilitaryHeroes.Cyberpunk.Mask.GlassesType08',
                ]
            }
        },
        Earrings: {
            ROULETTE_GOLD_COST: 60,
            ROULETTE_CRYSTAL_COST: 6,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'Common.Common.Earrings.Type1',
                    'Common.Common.Earrings.Type2',
                    'Common.Common.Earrings.Type3',
                    'Common.Common.Earrings.Type4',
                    'Common.Common.Earrings.Type5',
                    'Common.Common.Earrings.Type6',
                    'Common.Common.Earrings.Type7',
                    'Common.Common.Earrings.Type8',
                    'Common.Common.Earrings.Type9',
                    'Common.Common.Earrings.Type10'
                ],
                RARE: [
                    'Common.Common.Earrings.Type11',
                    'Common.Common.Earrings.Type12',
                    'Common.Common.Earrings.Type13',
                    'Common.Common.Earrings.Type14',
                    'Common.Common.Earrings.Type15',
                    'Common.Common.Earrings.Type16',
                    'Common.Common.Earrings.Type17'
                ],
                EPIC: [],
                LEGENDARY: []
            }
        },
        //Paint 아직 미구현
        Paint: {
            ROULETTE_GOLD_COST: 50,
            ROULETTE_CRYSTAL_COST: 5,
            ROULETTE_POOL_SIZE: 8,
            TIERS: {
                COMMON: [
                    'FF0000FF', 'FF4400FF', 'FF8800FF', 'FFCC00FF',
                    'FFFF00FF', 'CCFF00FF', '88FF00FF', '44FF00FF',
                    '00FF00FF', '00FF44FF', '00FF88FF', '00FFCCFF',
                    '00FFFFFF', '00CCFFFF', '0088FFFF', '0044FFFF',
                    '0000FFFF', '4400FFFF', '8800FFFF', 'CC00FFFF',
                    'FF00FFFF', 'FF00CCFF', 'FF0088FF', 'FF0044FF',
                    'FFFFFFFF', 'CCCCCCFF', '999999FF', '666666FF',
                    '333333FF', '000000FF', '963200FF', '00C8FFFF'
                ],
                RARE: [
                    'FFB3BAFF', 'FFDFBAFF', 'FFFFBAFF', 'BAFFC9FF',
                    'BAE1FFFF', 'C9BAFFFF', 'FFBAF3FF', 'FFE4E1FF',
                    'E6E6FAFF', 'F0E68CFF', 'E0FFFFFF', 'FFE4B5FF',
                    'FFDAB9FF', 'FFB6C1FF', 'DDA0DDFF', 'B0E0E6FF'
                ],
                EPIC: [
                    'FF1493FF', 'FF69B4FF', 'FF6347FF', 'FF7F50FF',
                    '32CD32FF', '00CED1FF', '1E90FFFF', '9370DBFF',
                    'FF8C00FF', 'DC143CFF', '00FA9AFF', '9400D3FF'
                ],
                LEGENDARY: [
                    'FFD700FF', 'C0C0C0FF', 'CD7F32FF', 'E5E4E2FF'
                ]
            }
        }
    }
};