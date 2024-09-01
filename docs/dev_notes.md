## Documenting Informs behaviour

Different rulebooks have different defaults:
* Before/Check/Carryout/Report - Make No Descision aka Continue
* After - Rule suceeds
* Instead - Rule fails

The semantics of continue and sucess/failure are the same for each rulebook. It'll keep evaluating rules in that book until it gets a success or failure, then stop.

But in evaluating an action, the result of different rulebooks interpret success/failure differently.
* Before - success/failure stops the overall action
* Instead - success/failure stops the overall action
* Check - success/failure stops the overall action
* Carry Out - always continues
* After - success/failure stops the overall action
* Report - irrelevant

 
Note, "Stop the action" is not the same as "rule succeeds/fails", it disables some inform 7 housekeeping. https://inform-7-handbook.readthedocs.io/en/latest/chapter_4_actions/rulebooks_&_stop_the_action/

So I guess somehwere internal to inform, it has something like

```
Abide by the before rulebook;
Some internal stuff..
Abide by the instead rulebook;
Abide by the check rulebook.
Follow the carry out rulebook;
Abide by the after rulebook;
Abide by the report rulebook;
```

(these can be found in the `action processing` and `specific action processing` rulebooks in the standard rules.)

(https://inform-7-handbook.readthedocs.io/en/latest/chapter_4_actions/action_processing__summary/)

# TODO Variables

Ther's not a great story for replacing rulebook variables. Also, inform gets good milage out of
dynamically scoped variablse like [the noun].
Do we want something similar?

# TODO Nice APIs  to implement

.TypeMatch<Foo, Bar>() as shorthand for .When((t1, t2) => t1 is Foo and t2 is Bar) + smart casting by the builder

.ComponentMatch<Foo>() as shorthand for .When((t1) => t1.TryGetComponent<Foo>()) + smart casting by the builder (Unity only)

Some sort of accumulation API where the results of different rules are combined, rather than taking the first

Support coroutines framework. Is this a special type of accumulation?

# TODO Activity
 - a triple of rulebooks, plus a dynamic variable for "while <activity>" predicate.
