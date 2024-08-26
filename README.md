C# library implementing something like Inform 7 rulebooks

What are rulebooks?

Rulebooks are essentially a fancy form of C#'s Func<> and Action<> generics.

While Func<> and Action<> are opaque objects that cannot be edited once generated, 
rulebooks are built out of individual rules, and are inspectable mutable.

Rulebooks unified access to a bunch of useful programming patterns, including events, multiple dispatch, modding and code weaving.

# Example

It's common to write out game logic

```csharp
void EatFood(Object t)
{
    if(!t.Edible)
    {
        Say("You can't eat that");
        return;
    }
    if(t.Poisoned)
    {
        SetStatus("poisoned");
    }

    Say($"You eat the {t}");
    IncreaseHealth(t.HealthRecovery);
}

EatFood(thePizza);
```

With rulebooks, you can instead do

```csharp
var EatFood = new ActionBook<Object>();

EatFood.When(t=>!t.Edible).AtFirst().Instead(t => Say("You can't eat that"));
EatFood.When(t=>t.Poisoned).Do(t => { SetStatus("Poisoned"); }).Named("Poisoned");
EatFood.AtLast().Do(t => {
    Say($"You eat the {t}");
    IncreaseHealth(t.HealthRecovery);
});

EatFood.Invoke(thePizza);
```

In this toy example, we haven't done much more than made the code less readable than the original.
But this registers 3 different rules, they don't need to be placed near each other or registered in order.

And we can later start modifying those rules themselves as rulebooks


EatFood["Poisoned"].When(t=>Player.HasPoisonImmunity).Instead(() => {});


# Rules for sorting rulebooks
# TODO



# TODO - phases

Inform has multiple rulebooks for one action. Do we want to roll that in somehow?
It would be great to treat these as sort orders, but that's not so.

If an instead rule succeeds, it skips all other instead rules, and then stops the action.
If a carry out rule succeeds, it skips all other carry out rules and moves onto the next phase (check).

(also, inform has different rulebooks default to different behaviours of stop/continue, but we make users explicit there)


# TODO Variables

Ther's not a great story for replacing rulebook variables. Also, inform gets good milage out of
dynamically scoped variablse like [the noun].
Do we want something similar?


# Translation of Terms from Inform 7

Rulebook basis - args
Rulebook variables - no equivalent? Use default args?

Success / Failure - You can throw, or you can make a FuncBook that returns true/false
Named outcomes - Return values, using an enum
Make no descision - same as Continue
Rulebooks producing value - Return values (FuncBook)
AbideBy - .DoWithResult(() => { return otherRule.AbideBy()})
AbideBy (multiple rules) - TODO - a bit awkward atm
AbideByAnonymously

[the noun] [the second noun] etc - Use args. Note, it's probably better to create an Action object with several details and have a single arged rulebook.
I think technically Inform 7 uses dynamically scoped variables, but C# doesn't have them. You can approximate with globals if you like

Before/After/Instead/Carry Out etc - no equivalent? Create multiple rulebooks