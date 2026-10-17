using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTransportationSystem
{
    public class TotalCalculationObject
    {
        public Dictionary<GameObject, CalculationForGameObject> _calculations = new Dictionary<GameObject, CalculationForGameObject>();

        public TotalCalculationObject()
        {

        }

        public CalculationForGameObject GetCalculationForGameObject(GameObject gameObject)
        {
            CalculationForGameObject calculationForGameObject;
            var worked = _calculations.TryGetValue(gameObject, out calculationForGameObject);
            if (!worked)
            {
                calculationForGameObject = new CalculationForGameObject();
                _calculations.Add(gameObject, calculationForGameObject);
            }
            return calculationForGameObject;
        }



    }
}
