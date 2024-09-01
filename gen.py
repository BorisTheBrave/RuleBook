# Run this module to generate all the Func and Action variants for different parameter counts.

import os
from stat import S_IREAD,S_IRGRP,S_IROTH

def transform_action(src, dst):
    lines = open(src).readlines()

    results = []
    results.append(f"// This file is generated, please edit source in {src}\n")
    results.append("#define IS_ACTION\n")
    for line in lines:
        line = line.replace(", TRet", "")
        line = line.replace(", TRet value", "")
        line = line.replace("TRet value", "")
        line = line.replace("FuncBook", "ActionBook")
        line = line.replace("FuncRule", "ActionRule")

        results.append(line)

    if os.path.exists(dst):
        os.chmod(dst, 0o777)
        os.remove(dst)
    open(dst, 'w').writelines(results)
    os.chmod(dst, S_IREAD|S_IRGRP|S_IROTH)   

def transform_n(src, dst, n):
    lines = open(src).readlines()

    results = []
    if "This file is generated" not in lines[0]:
        results.append(f"// This file is generated, please edit source in {src}\n")
    r = range(1, n+1)
    for line in lines:
        
        if n == 0:
            line = line.replace("TArg1 arg1, ", "")
            line = line.replace("TArg1, ", "")
            line = line.replace("arg1, ", "")
        line = line.replace("TArg1 arg1", "!TARGS!")
        line = line.replace("TArg1", ", ".join([f"TArg{i}" for i in r]))
        line = line.replace("arg1", ", ".join([f"arg{i}" for i in r]))
        line = line.replace("!TARGS!", ", ".join([f"TArg{i} arg{i}" for i in r]))
        if n == 0:
            line = line.replace("ActionBook<>", "ActionBook")
            line = line.replace("ActionBook{}", "ActionBook")
            line = line.replace("ActionRule<>", "ActionRule")
            line = line.replace("ActionRule{}", "ActionRule")
            line = line.replace("Action<>", "Action")
            line = line.replace(", )", ")")
            line = line.replace(", >", ">")
            line = line.replace(", }", "}")

        results.append(line)

    if os.path.exists(dst):
        os.chmod(dst, 0o777)
        os.remove(dst)
    open(dst, 'w').writelines(results)
    os.chmod(dst, S_IREAD|S_IRGRP|S_IROTH)


if __name__ == '__main__':
    if not os.path.exists("RuleBook/Gen"):
        os.makedirs("RuleBook/Gen")
    transform_action('RuleBook/FuncRule.1.cs', f'RuleBook/Gen/ActionRule.1.cs')
    transform_action('RuleBook/FuncBook.1.cs', f'RuleBook/Gen/ActionBook.1.cs')
    for n in range(0, 8+1):
        if n == 1:
            continue
        transform_n('RuleBook/FuncRule.1.cs', f'RuleBook/Gen/FuncRule.{n}.cs', n)
        transform_n('RuleBook/FuncBook.1.cs', f'RuleBook/Gen/FuncBook.{n}.cs', n)
        transform_n('RuleBook/Gen/ActionRule.1.cs', f'RuleBook/Gen/ActionRule.{n}.cs', n)
        transform_n('RuleBook/Gen/ActionBook.1.cs', f'RuleBook/Gen/ActionBook.{n}.cs', n)