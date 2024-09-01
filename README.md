# RuleBook

A C# library for modular functions, and actions, heavily inspired by [Inform 7](https://ganelson.github.io/inform-website/)'s [Rulebooks](https://ganelson.github.io/inform-website/book/WI_19_1.html).

**What are rulebooks?**

Rulebooks are essentially a fancy form of C#'s `Func<>` and `Action<>` generics.

`Func<>` and `Action<>` can hold a reference to a C# method, or an lambda expression/statement. But the thing they hold is essentially a black box - you cannot do much with it except run it, or check equality.

RuleBook provies `FuncBook<>` and `ActionBook<>`, which work similarly to their counterparts. But these rulebook objects are built out of individual rules, which can be individually inspected and mutated.

Overall, rulebooks give a systematic way of handling a bunch of useful programming patterns, including events, multiple dispatch, modding and code weaving.

# Example

All that's a bit abstract, so let's look at a concrete example. Suppose you are coding some logic for a game. You might have a function like:

```csharp
void EatFood(Item t)
{
    if (!t.Edible)
    {
        Say("You can't eat that");
        return;
    }
    if (t.Poisoned)
    {
        SetStatus("poisoned");
    }

    Say($"You eat the {t}.");
    IncreaseHealth(t.HealthRecovery);
}

EatFood(thePizza);
```

With rulebooks, you can instead do

```csharp
var EatFood = new ActionBook<Item>();

EatFood.AddRule().When(t => !t.Edible).At(-100).Instead(t => { Say("You can't eat that"); });
EatFood.AddRule().Named("Poisoned").When(t => t.Poisoned).Do(t => { SetStatus("poisoned"); });
EatFood.AddRule().Do(t => {
    Say($"You eat the {t}.");
    IncreaseHealth(t.HealthRecovery);
});

EatFood.Invoke(thePizza);
```

In this toy example, we haven't done much more than made the code less readable than the original!

But there are advantages to this approach. We've registered 3 separate rules for this rulebook. Those rules could be registered anywhere in the code base in any order.

That let's us organize the code better. You can place code for handling eating a magic cake with all the other code for magic cakes, you don't have to put every special case inside the EatFood method.

And we can later start modifying those rules themselves as rulebooks, if we want to override reglar behaviour.

```csharp
// Ignore the Poisoned rule when the player is immune.
EatFood["Poisoned"].Rulebook.AddRule().When(t => Player.HasPoisonImmunity).Stop();
```

This let's you organize your code in a very modular way.

You can find more examples in the [Examples folder](RuleBook.Test/Examples), or read the [full docs here](docs/index.md).

# Installation

No NuGet package for now, let me know if that is desirable.

It's easiset to use a prebuilt .dll or a .zip containing the sources. Either can be found in GitHub releases.

If you want to build from source, you will need to run `gen.py` to generate all the variant classes.