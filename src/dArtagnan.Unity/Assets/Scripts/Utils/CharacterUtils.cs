using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Assets.HeroEditor4D.InventorySystem.Scripts.Data;
using Costume;
using UnityEngine;

namespace Utils
{
    public static class CharacterUtils
    {
        public static EquipmentPart StringToEquipmentPart(this string str) => str switch
            {
                "Helmet" => EquipmentPart.Helmet,
                "Vest" => EquipmentPart.Vest,
                "Bracers" => EquipmentPart.Bracers,
                "Leggings" => EquipmentPart.Leggings,
                "Armor" => EquipmentPart.Armor,
                "Firearm1H" => EquipmentPart.Firearm1H,
                "Mask" => EquipmentPart.Mask,
                "Earrings" => EquipmentPart.Earrings,
                _ => throw new ArgumentOutOfRangeException(nameof(str), str, null)
            };

        public static BodyPart StringToBodyPart(this string str) => str switch
            {
                "Body" => BodyPart.Body,
                "Head" => BodyPart.Head,
                "Hair" => BodyPart.Hair,
                "Ears" => BodyPart.Ears,
                "Eyebrows" => BodyPart.Eyebrows,
                "Eyes" => BodyPart.Eyes,
                "Mouth" => BodyPart.Mouth,
                "Beard" => BodyPart.Beard,
                "Makeup" => BodyPart.Makeup,
                _ => throw new ArgumentOutOfRangeException(nameof(str), str, null)
            };

        public static Paint StringToPaint(this string str) => str switch
            {
                "Body_Paint" => Paint.Body_Paint,
                "Eyes_Paint" => Paint.Eyes_Paint,
                "Hair_Paint" => Paint.Hair_Paint,
                "Beard_Paint" => Paint.Beard_Paint,
                _ => throw new ArgumentOutOfRangeException(nameof(str), str, null)
            };

        public static (Dictionary<BodyPart, ItemSprite> bodyParts, Dictionary<Paint, Color> paints,
            Dictionary<EquipmentPart, ItemSprite> equipments) ParseAppearanceInformation(
                this IDictionary<string, string> info,
                SpriteCollection spriteCollection)
        {
            var bodyParts = new Dictionary<BodyPart, ItemSprite>();
            var equipments = new Dictionary<EquipmentPart, ItemSprite>();
            var paints = new Dictionary<Paint, Color>();
            foreach (var pair in info)
            {
                if (Enum.GetNames(typeof(EquipmentPart)).Contains(pair.Key))
                {
                    var found = pair.Value == "None" ? null : spriteCollection.AllSprites.First(s => s.Id == pair.Value);
                    equipments[pair.Key.StringToEquipmentPart()] = found;
                }
                else if (Enum.GetNames(typeof(BodyPart)).Contains(pair.Key))
                {
                    var found = pair.Value == "None" ? null : spriteCollection.AllSprites.First(s => s.Id == pair.Value);
                    bodyParts[pair.Key.StringToBodyPart()] = found;
                }
                else if (Enum.GetNames(typeof(Paint)).Contains(pair.Key))
                {
                    var s = pair.Value;
                    if (!s.StartsWith('#'))
                        s = '#' + s;
                    if (!ColorUtility.TryParseHtmlString(s, out var c))
                    {
                        throw new Exception($"Invalid color: {s}");
                    }
                    paints[pair.Key.StringToPaint()] = c;
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(pair.Key), pair.Key, null);
            }
            return (bodyParts, paints, equipments);
        }
        
        public static void UpdateCostumeByData(this Character4D character,
            IDictionary<BodyPart, ItemSprite> bodyParts,
            IDictionary<Paint, Color> paints,
            IDictionary<EquipmentPart, ItemSprite> equipments)
        {
            var appearance = new CharacterAppearance();
            foreach (var pair in bodyParts)
            {
                switch (pair.Key)
                {
                    case BodyPart.Beard:
                        appearance.Beard = pair.Value?.Id;
                        break;
                    case BodyPart.Body:
                        appearance.Body = pair.Value?.Id;
                        break;
                    case BodyPart.Ears:
                        appearance.Ears = pair.Value?.Id;
                        break;
                    case BodyPart.Eyebrows:
                        appearance.Eyebrows = pair.Value?.Id;
                        break;
                    case BodyPart.Eyes:
                        appearance.Eyes = pair.Value?.Id;
                        break;
                    case BodyPart.Makeup:
                        appearance.Eyebrows = pair.Value?.Id;
                        break;
                    case BodyPart.Hair:
                        appearance.Hair = pair.Value?.Id;
                        break;
                    case BodyPart.Mouth:
                        appearance.Mouth = pair.Value?.Id;
                        break;
                    case BodyPart.Head:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            foreach (var pair in paints)
            {
                switch (pair.Key)
                {
                    case Paint.Body_Paint:
                        appearance.BodyColor = pair.Value;
                        break;
                    case Paint.Eyes_Paint:
                        appearance.EyesColor = pair.Value;
                        break;
                    case Paint.Hair_Paint:
                        appearance.HairColor = pair.Value;
                        break;
                    case Paint.Beard_Paint:
                        appearance.BeardColor = pair.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            appearance.Setup(character);
            foreach (var pair in equipments)
            {
                character.Equip(pair.Value, pair.Key);
            }
        }
    
    }
}