import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import re
from collections import Counter

# 读取 CSV 文件（假设文件名为 unity_projects.csv）
df = pd.read_csv(r'F:\--CodeRepo\--CodeRepo\Paper Reading\____Writing\Data Files\unity_projects_summary3.csv')

# 提取主版本号，例如 2020.x、2021.x、5.x
def extract_major_version(version_str):
    match = re.match(r'(\d{1,4})\.\d+\.\d+f\d+', str(version_str))
    if match:
        return f"{match.group(1)}.x"
    return "Unknown"

df['MajorVersion'] = df['UnityVersion'].apply(extract_major_version)

# 统计每个主版本的项目数量
version_counts = df['MajorVersion'].value_counts().sort_index()

# 可视化
sns.set(style="whitegrid")
palette = sns.color_palette("husl", len(version_counts))

plt.figure(figsize=(10, 6))
bars = plt.bar(version_counts.index, version_counts.values, color=palette)

# 添加数据标签
for bar in bars:
    yval = bar.get_height()
    plt.text(bar.get_x() + bar.get_width()/2, yval + 0.5, int(yval),
             ha='center', va='bottom', fontsize=10)

# 图表设置
plt.title("Unity Version Distribution of Projects", fontsize=14)
plt.xlabel("Unity Version (Major)", fontsize=12)
plt.ylabel("Number of Projects", fontsize=12)
plt.xticks(rotation=45)
plt.tight_layout()
plt.grid(axis='y', linestyle='--', alpha=0.7)

# 保存并展示
plt.savefig("unity_version_distribution.png", dpi=300)
plt.show()
