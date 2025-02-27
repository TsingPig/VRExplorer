import os
import subprocess

# 定义存放仓库的目标文件夹路径
target_directory = "F:\--CodeRepo\--CodeRepo\TsingPig_Lab\LLM"

# 确保目标文件夹存在
if not os.path.exists(target_directory):
    os.makedirs(target_directory)

# 仓库列表文件路径
file_path = "git_list.txt"  # 假设你的文件名是 repos_list.txt

# def clone_repo(repo_url):
#     try:
#         print(f"Cloning {repo_url}...")
#         subprocess.run(["git", "clone", repo_url], check=True)
#         print(f"Successfully cloned {repo_url}")
#     except subprocess.CalledProcessError:
#         print(f"Failed to clone {repo_url}")

def clone_repo(repo_url):
    try:
        print(f"Cloning {repo_url}...")
        subprocess.run(["git", "clone", "--depth", "1", repo_url], check=True)
        print(f"Successfully cloned {repo_url}")
    except subprocess.CalledProcessError as e:
        print(f"Failed to clone {repo_url}: {e}")
        # 重试逻辑
        print(f"Retrying {repo_url} with SSL disabled...")
        subprocess.run(["git", "-c", "http.sslVerify=false", "clone", "--depth", "1", repo_url])

# 读取文件并处理每一行
with open(file_path, "r") as file:
    for line in file:
        # 跳过空行或注释行
        if not line.strip() or line.startswith('#'):
            continue

        # 提取仓库 URL（假设每行的第一个元素是仓库 URL）
        columns = line.strip().split(',')
        repo_url = columns[0]

        # 切换到目标文件夹
        os.chdir(target_directory)

        # 克隆仓库
        clone_repo(repo_url)

        # 返回到脚本目录（如果需要的话）
        os.chdir('..')
