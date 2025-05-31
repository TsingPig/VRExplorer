import requests
import json
import base64

# GitHub Token
GITHUB_TOKEN = "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"
HEADERS = {"Authorization": f"token {GITHUB_TOKEN}"}


# 检查一个仓库是否符合条件
def check_repo_for_unity_xr_toolkit(repo_url):
    # 获取仓库的用户和项目名，并移除可能存在的多余路径
    owner_repo = repo_url.replace("https://github.com/", "").split('/tree/')[0].strip("/")

    try:
        owner, repo = owner_repo.split("/")
    except ValueError:
        print(f"Invalid repository URL format: {repo_url}")
        return False

    # 获取默认分支名称
    branch_url = f"https://api.github.com/repos/{owner}/{repo}"
    branch_response = requests.get(branch_url, headers = HEADERS)

    if branch_response.status_code == 200:
        repo_data = branch_response.json()
        default_branch = repo_data.get("default_branch", "main")
    else:
        print(f"Error fetching default branch for {repo_url} - {branch_response.status_code}")
        return False

    # 构建 API URL，获取仓库的文件树
    api_url = f"https://api.github.com/repos/{owner}/{repo}/git/trees/{default_branch}?recursive=1"
    response = requests.get(api_url, headers = HEADERS)

    if response.status_code == 200:
        tree_data = response.json()

        manifest_found = False
        for item in tree_data.get("tree", []):
            if item["path"] == "Packages/manifest.json":
                manifest_found = True
                file_url = item["url"]
                file_response = requests.get(file_url, headers = HEADERS)

                if file_response.status_code == 200:
                    manifest_data = file_response.json()
                    content_base64 = manifest_data.get('content', '')
                    content_json = base64.b64decode(content_base64).decode('utf-8')

                    try:
                        manifest_json = json.loads(content_json)
                        dependencies = manifest_json.get("dependencies", {})

                        if "com.unity.xr.interaction.toolkit" in dependencies:
                            print(f"Found com.unity.xr.interaction.toolkit in {repo_url}")
                            return True
                        else:
                            print(f"com.unity.xr.interaction.toolkit not found in {repo_url}")
                    except json.JSONDecodeError:
                        print(f"Error decoding JSON for {repo_url}")

        if not manifest_found:
            print(f"manifest.json not found in {repo_url}")
        return False
    elif response.status_code == 404:
        print(f"Error: Repository {repo_url} not found (404)")
        return "404"
    else:
        print(f"Error: Unable to fetch data for {repo_url} - {response.status_code}")
        return False


# 读取 _result.txt 文件并处理每个 URL
def process_repositories(input_file = "failed_repositories.txt", output_file = "valid_repositories.txt",
                         failed_file = "failed_repositories.txt"):
    with open(input_file, 'r') as file:
        repo_urls = [line.strip() for line in file.readlines()]

    valid_repos = []
    failed_repos = []

    for repo_url in repo_urls:
        print(f"Checking repository: {repo_url}")
        check_result = check_repo_for_unity_xr_toolkit(repo_url)
        if check_result == True:
            valid_repos.append(repo_url)
        elif check_result == "404":
            failed_repos.append(repo_url)

    # 输出符合条件的仓库
    with open(output_file, 'w') as file:
        for valid_repo in valid_repos:
            file.write(valid_repo + '\n')

    # 输出失败的仓库
    with open(failed_file, 'w') as file:
        for failed_repo in failed_repos:
            file.write(failed_repo + '\n')

    print(f"Found {len(valid_repos)} valid repositories.")
    print(f"Found {len(failed_repos)} failed repositories.")


# 调用处理函数
process_repositories()
