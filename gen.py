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

def transform(src, dst, n):
    lines = open(src).readlines()

    results = []
    results.append(f"// This file is generated, please edit source in {src}\n")
    r = range(1, n+1)
    for line in lines:
        line = line.replace("TArg1 arg1", "!TARGS!")
        line = line.replace("TArg1", ", ".join([f"TArg{i}" for i in r]))
        line = line.replace("arg1", ", ".join([f"arg{i}" for i in r]))
        line = line.replace("!TARGS!", ", ".join([f"TArg{i} arg{i}" for i in r]))

        results.append(line)

    if os.path.exists(dst):
        os.chmod(dst, 0o777)
        os.remove(dst)
    open(dst, 'w').writelines(results)
    os.chmod(dst, S_IREAD|S_IRGRP|S_IROTH)


if __name__ == '__main__':
    transform_action('RuleBook/FuncRule.1.cs', f'RuleBook/Gen/ActionRule.1.cs')
    transform_action('RuleBook/FuncBook.1.cs', f'RuleBook/Gen/ActionBook.1.cs')
    for n in range(2, 8+1):
        transform('RuleBook/FuncRule.1.cs', f'RuleBook/Gen/FuncRule.{n}.cs', n)
        transform('RuleBook/FuncBook.1.cs', f'RuleBook/Gen/FuncBook.{n}.cs', n)
        transform('RuleBook/Gen/ActionRule.1.cs', f'RuleBook/Gen/ActionRule.{n}.cs', n)
        transform('RuleBook/Gen/ActionBook.1.cs', f'RuleBook/Gen/ActionBook.{n}.cs', n)