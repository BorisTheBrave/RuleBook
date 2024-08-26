import os
from stat import S_IREAD,S_IRGRP,S_IROTH

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
    for n in range(2, 8+1):
        transform('RuleBook/FuncRule.1.cs', f'RuleBook/FuncRule.{n}.cs', n)
        transform('RuleBook/FuncBook.1.cs', f'RuleBook/FuncBook.{n}.cs', n)