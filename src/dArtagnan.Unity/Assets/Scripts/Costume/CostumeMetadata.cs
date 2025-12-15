using System;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;

namespace Costume
{
    [Serializable]
    public struct CostumeMetadata
    {
        public int id;
        public string name; 
        public Character4D character;
    }
}