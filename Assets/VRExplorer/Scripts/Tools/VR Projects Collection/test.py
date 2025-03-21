import requests

# GitHub Token
GITHUB_TOKEN = "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"
HEADERS = {"Authorization": f"token {GITHUB_TOKEN}"}


def fetch_repo_info(repo_url):
    """
    Fetch repository information using GitHub API.
    """
    # Extract owner and repo name
    owner_repo = repo_url.replace("https://github.com/", "").split('/tree/')[0].strip("/")
    try:
        owner, repo = owner_repo.split("/")
    except ValueError:
        print(f"Invalid repository URL format: {repo_url}")
        return None

    # Fetch repository data
    repo_api_url = f"https://api.github.com/repos/{owner}/{repo}"
    response = requests.get(repo_api_url, headers=HEADERS)

    if response.status_code == 200:
        repo_data = response.json()
        # Fetch commit data for the default branch
        default_branch = repo_data.get("default_branch", "main")
        commits_api_url = f"https://api.github.com/repos/{owner}/{repo}/commits?sha={default_branch}&per_page=1"
        commits_response = requests.get(commits_api_url, headers=HEADERS)
        commit_count = int(commits_response.headers.get("Link", "0").split("&page=")[-1].split(">")[0]) if "Link" in commits_response.headers else 0

        # Gather repository data
        return {
            "url": repo_url,
            "stars": repo_data.get("stargazers_count", 0),
            "commits": commit_count,
            "branches": repo_data.get("branches", 0),
            "forks": repo_data.get("forks_count", 0),
            "open_issues": repo_data.get("open_issues_count", 0),
        }
    else:
        print(f"Error fetching repository data for {repo_url} - {response.status_code}")
        return None


def process_repositories(input_file="_result.txt", output_file="result_sorted.txt"):
    """
    Process repository URLs, fetch information, and sort based on criteria.
    """
    with open(input_file, 'r') as file:
        repo_urls = [line.strip() for line in file.readlines()]

    repo_infos = []
    for repo_url in repo_urls:
        print(f"Processing repository: {repo_url}")
        repo_info = fetch_repo_info(repo_url)
        if repo_info:
            repo_infos.append(repo_info)

    # Sort repositories by stars (desc) and commits (desc)
    repo_infos.sort(key=lambda x: (-x["stars"], -x["commits"]))

    # Write results to file
    with open(output_file, 'w') as file:
        file.write("URL, Stars, Commits, Branches, Forks, Open Issues\n")
        for info in repo_infos:
            file.write(f"{info['url']}, {info['stars']}, {info['commits']}, {info['branches']}, {info['forks']}, {info['open_issues']}\n")

    print(f"Results saved to {output_file}")


# Run the process
process_repositories()
