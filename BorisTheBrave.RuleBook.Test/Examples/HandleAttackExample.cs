using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BorisTheBrave.RuleBook.Test.Examples
{
    internal class HandleAttackExample
    {
        // For simplicity, I omit a type hierarchy or component system,
        // but those could be easily referenced.
        internal class GameObject
        {
            public string Type;
            public bool IsIntangible;
            public int Armour;
            public int Thorns;
            public int Health;
            public int Strength;
            public string WeaponType;
        }

        internal class AttackEvent
        {
            public GameObject Attacker;
            public GameObject Target;
            public int? DamageAmount;
            public bool IsRebound;
        }

        internal class Tutorial
        {
            public static int Phase;
        }
        void ShowTutorialText(string s) { }

        ActionBook<AttackEvent> handleAttack = new ();

        // I've listed all the rules in one place,
        // but feel free to imagine that the rules could actually be spread out over your whole codebase, or in mod packs, etc.
        [SetUp]
        public void SetUp()
        {
            // Initial damage defaults to attacker strength
            handleAttack.AddRule()
                .Named("initial damage")
                .At(-1000)
                .Do(e => e.DamageAmount ??= e.Attacker.Strength);

            // Except pistols, which do fixed damage
            // Note this uses "Instead" because it is overriding the initial damage rule.
            handleAttack["initial damage"].Rulebook.AddRule()
                .Named("pistol initial damage")
                .At(-10)
                .When(x => x.Attacker.WeaponType == "pistol")
                .Instead(e => e.DamageAmount ??= 10);

            // When an enemy is intangible, damage is reduced to 1.
            handleAttack.AddRule()
                .Named("intangible")
                .At(-100)
                .When(e => e.Target.IsIntangible)
                .Do(e => e.DamageAmount = 1);

            // When an enemy recieves damage, it deals damage back to player.
            // In this case we could simply check e.DamageAmount after cont(e)
            // but this example shows another way.
            // Note that intangible and thorns have the same sort order.
            // We don't care which order they run in.
            handleAttack.AddRule()
                .Named("thorns")
                .At(-100)
                .When(e => e.Target.Thorns > 0 && !e.IsRebound)
                .Wrap((cont, e) =>
                {
                    var healthBefore = e.Target.Health;
                    cont(e);
                    if (e.Target.Health < healthBefore)
                    {
                        handleAttack.Invoke(new AttackEvent
                        {
                            Attacker = e.Target,
                            Target = e.Attacker,
                            DamageAmount = e.Target.Thorns,
                            IsRebound = true,
                        });
                    }
                });

            // Want this to run before intangible.
            // If it ran after, then 1 armor + intangible would be sufficient to no damage every hit.
            handleAttack.AddRule()
                .Named("armour")
                .At(-200)
                .Do(e =>
                {
                    e.DamageAmount -= Math.Min(e.DamageAmount.Value, e.Target.Armour);
                    // Possibly trigger some sort of animation here
                });

            handleAttack.AddRule()
                .Named("normal damage")
                .Do(e =>
                {
                    // In a more complex game, actually dealing damage would be a separate rulebook
                    // to handle animations, dying, etc
                    e.Target.Health -= Math.Min(e.DamageAmount.Value, e.Target.Health);
                });

            // An advantage of rulebooks is you can put in hyperspecific rules without polluting the rest of the code
            // Here are some ideas

            // Special case tutorials, e.g. to detect if a player has done an action correctly.
            handleAttack.AddRule()
                .When(e => Tutorial.Phase == 103)
                .Do(e =>
                {
                    Tutorial.Phase++;
                    ShowTutorialText("...");
                });

            // Special abilities don't need to be explicitly given status effects and other game state,
            // you can just make special rules
            handleAttack.AddRule()
                .Named("boss gains strength on attacked")
                .When(e => e.Target.Type == "the_boss")
                .Do(e =>
                {
                    e.Target.Strength += 1;
                });

            // This only triggers for attacks. In a more complex game,
            // This would go in a deal damage rulebook
            // so it can trigger for any source of damage.
            handleAttack.AddRule()
                .Named("slime splits at half health")
                .When(e => e.Target.Type == "slime")
                .At(100)
                .Do(e =>
                {
                    if(e.Target.Health <= 50)
                    {
                        // Handle splitting slime
                    }
                });
        }


        // Often it's handy to make wrapper functions for rulebooks so they are easy to call
        public void HandleAttack(GameObject attacker, GameObject target)
        {
            handleAttack.Invoke(new AttackEvent { Attacker = attacker, Target = target });
        }

        [Test]
        public void Run()
        {
            Console.WriteLine(handleAttack.FormatTree());
            /* Prints
                Abide by rulebook rule named "initial damage" with rules:
                  Rule named "pistol initial damage"
                  Rule named "original"
                Rule named "armour"
                Rule named "intangible"
                Wrap rule named "thorns"
                Rule named "e => Tutorial.Phase == 103"
                Rule named "boss gains strength on attacked"
                Rule named "normal damage"
                Rule named "slime splits at half health"
            */

            var player = new GameObject
            {
                Strength = 5,
                Health = 10,
            };
            var enemy = new GameObject
            {
                Health = 4,
                Armour = 2,
                Thorns = 1,
            };
            HandleAttack(player, enemy);

            Assert.That(player.Health, Is.EqualTo(9));
            Assert.That(enemy.Health, Is.EqualTo(1));
        }
    }
}
