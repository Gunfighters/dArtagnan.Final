using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "MonetaryUnitCollection", menuName = "d'Artagnan/MonetaryUnitCollection", order = 0)]
    public class MonetaryUnitCollection : ScriptableObject
    {
        public List<MonetaryUnitData> Units;

        public List<Sprite> CalculateUnitsByAmount(int balance)
        {
            var result = new List<Sprite>();
            foreach (var data in Units.OrderByDescending(unit => unit.threshold))
            {
                var count = balance / data.threshold;
                for (var i = 0; i < count; i++)
                    result.Add(data.icon);
                balance %= data.threshold;
            }
            return result;
        }
    }
}