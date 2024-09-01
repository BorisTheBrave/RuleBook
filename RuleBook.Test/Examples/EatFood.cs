using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleBook.Test.Examples
{
    internal class EatFood
    {
        public class Item
        {
            public bool Edible;
            public bool Poisoned;
            public int HealthRecovery;
        }

        void SetStatus(string s) { }
        void Say(string s) { }
        void IncreaseHealth(int hp) { }

        Item thePizza = new Item { Edible = true };

        [Test]
        public void Run()
        {
            var EatFood = new ActionBook<Item>();

            EatFood.AddRule().When(t => !t.Edible).At(-100).Instead(t => { Say("You can't eat that"); });
            EatFood.AddRule().Named("Poisoned").When(t => t.Poisoned).Do(t => { SetStatus("poisoned"); });
            // This is a very simple example. In practice, these would be two separate Do rules
            // so the behavious can be independently overriden
            EatFood.AddRule().Do(t => {
                Say($"You eat the {t}.");
                IncreaseHealth(t.HealthRecovery);
            });

            EatFood.Invoke(thePizza);
        }
    }
}
