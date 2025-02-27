import requests

# 替换为你的 GitHub Token
GITHUB_TOKEN = "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"
HEADERS = {"Authorization": f"token {GITHUB_TOKEN}"}

def load_filtered_urls(file_path):
    # 读取 vr_unity_vr.txt 文件，将其中的 URL 加入集合
    try:
        with open(file_path, 'r') as file:
            filtered_urls = {line.strip() for line in file.readlines()}
        return filtered_urls
    except FileNotFoundError:
        return set()


def search_github_repositories_with_topics(query, topics, per_page = 50, max_pages = 30, filter_file = "_result.txt"):
    base_url = "https://api.github.com/search/repositories"
    results = []

    # 加载过滤掉的仓库 URL
    filtered_urls = load_filtered_urls(filter_file)

    # 构建查询字符串，筛选包含指定标签的仓库
    topic_query = " ".join([f"topic:{topic}" for topic in topics])
    full_query = f"{query} {topic_query}"

    for page in range(1, max_pages + 1):
        params = {
            "q": full_query,
            "sort": "stars",  # 按星标排序
            "order": "desc",
            "per_page": per_page,
            "page": page
        }
        response = requests.get(base_url, headers = HEADERS, params = params)

        if response.status_code == 200:
            data = response.json()
            for item in data["items"]:
                repo_url = item["html_url"]
                # 如果仓库 URL 不在过滤列表中，才添加到结果中
                if repo_url not in filtered_urls:
                    results.append(repo_url)
            if "next" not in response.links:  # 如果没有下一页，停止
                break
        else:
            print(f"Error: {response.status_code}, {response.text}")
            break

    return results

if __name__ == "__main__":
    query = "Unity"
    topics = ["vr", "unity", "xr"]  # 需要筛选的标签
    repositories = search_github_repositories_with_topics(query, topics)
    print(f"Found {len(repositories)} repositories:")
    for repo in repositories:
        print(repo)



