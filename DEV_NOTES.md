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

## Documenting Informs behaviour

Different rulebooks have different defaults:
Before/Check/Carryout/Report - Make No Descision aka Continue
After - Rule suceeds
Instead - Rule fails

The semantics of continue and sucess/failure are the same for each rulebook. It'll keep evaluating rules in that book until it gets a success or failure, then stop.

But in evaluating an action, the result of different rulebooks interpret success/failure differently.
Before - success/failure stops the overall action
Instead - success/failure stops the overall action
Check - success/failure stops the overall action
Carry Out - always continues
After - success/failure stops the overall action
Report - irrelevant

 
Note, "Stop the action" is not the same as "rule succeeds/fails", it disables some inform 7 housekeeping. https://inform-7-handbook.readthedocs.io/en/latest/chapter_4_actions/rulebooks_&_stop_the_action/

So I guess somehwere internal to inform, it has something like

Abide by the before rulebook;
Some internal stuff..
Abide by the instead rulebook;
Abide by the check rulebook.
Follow the carry out rulebook;
Abide by the after rulebook;
Abide by the report rulebook;

(these can be found in the action-processing rules and specific action processing rules in the standard rules.)

(https://inform-7-handbook.readthedocs.io/en/latest/chapter_4_actions/action_processing__summary/)

# TODO Variables

Ther's not a great story for replacing rulebook variables. Also, inform gets good milage out of
dynamically scoped variablse like [the noun].
Do we want something similar?

# TODO Nice API this to implement

FuncBook/ActionBook to mimic Func and Action respectively.

.TypeMatch<Foo, Bar>() as shorthand for .When((t1, t2) => t1 is Foo and t2 is Bar) + smart casting by the builder

.ComponentMatch<Foo>() as shorthand for .When((t1) => t1.TryGetComponent<Foo>()) + smart casting by the builder (Unity only)

Some sort of accumulation API where the results of different rules are combined, rather than taking the first

Support Task framework. Mix and match?

Support coroutines framework. Is this a special type of accumulation?

Wrapping rules. So you can write middleware type rules, not just linear rules

Recursive rulebooks: Rules them selves should be promotable to rulebooks

# TODO

Use ClalerArgumentExpression for unnamed rules

# TODO Activity
 - a triple of rulebooks, plus a dynamic variable for "while <activity>" predicate.

# TODO - Follow/Abide

Make these easier. Two uses - inserting a rule that directly does this, and from C# code.


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

