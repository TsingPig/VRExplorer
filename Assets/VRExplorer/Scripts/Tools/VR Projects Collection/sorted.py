def process_filter_file(file_path):
    try:
        # 读取文件并去重
        with open(file_path, 'r') as file:
            urls = {line.strip() for line in file.readlines()}  # 使用集合去重

        # 对 URL 进行排序
        sorted_urls = sorted(urls)

        # 覆盖原文件，写入去重且排序后的 URL
        with open(file_path, 'w') as file:
            for url in sorted_urls:
                file.write(url + '\n')

        return sorted_urls
    except FileNotFoundError:
        print(f"Error: {file_path} not found.")
        return []


# 示例调用
sorted_urls = process_filter_file('_result.txt')
print(sorted_urls)
