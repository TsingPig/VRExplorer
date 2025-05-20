l1, l2 = [], []

# 读取 _result.txt（提取每行的项目名）
with open(r'.\VR Projects Collection\_result.txt', 'r', encoding='utf-8') as file:
    for line in file:
        line = line.strip()  # 去掉换行符
        if line:  # 跳过空行
            # 假设每行是 GitHub URL，提取最后一部分作为项目名
            project_name = line.split('/')[-1]
            l1.append(project_name)

# 读取 filter.txt（去掉行首的 *）
with open(r'.\VR Projects Collection\filter.txt', 'r', encoding='utf-8') as file:
    for line in file:
        line = line.strip()
        if line:
            if line.startswith('*'):
                line = line[1:]  # 去掉行首的 *
            l2.append(line)

l1.sort()
l2.sort()
l1 = set(l1)
l2 = set(l2)
print(l1 - l2)
print(l2 - l1)
print("l1 (项目名):", l1)
print("l2 (过滤后):", l2)