using System.Collections.Generic;
using UnityEngine;

namespace Costume
{
    [CreateAssetMenu(fileName = "CostumeCollection", menuName = "d'Artagnan/Costume Collection", order = 0)]
    public class CostumeCollection : ScriptableObject
    {
        public List<CostumeMetadata> costumes;
    }
}