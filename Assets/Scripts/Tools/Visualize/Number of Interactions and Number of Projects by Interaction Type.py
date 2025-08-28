import pandas as pd
import matplotlib.pyplot as plt
import numpy as np

# 读CSV
df = pd.read_csv("../interaction_stats.csv")

# Project Coverage（计数）
project_counts = (df.drop(columns=['Project']) != 0).sum()

# 按 Project Coverage 排序
sorted_index = project_counts.sort_values(ascending=True).index
project_counts = project_counts[sorted_index]

# 总计也按照相同顺序排序
total_counts = df.drop(columns=['Project']).sum()
total_counts = total_counts[sorted_index]

# x 轴位置
x = np.arange(len(total_counts))
width = 0.4

fig, ax1 = plt.subplots(figsize=(6,5))

# ------------------------
# 上方倒挂柱子：Total Interactions
# 使用蓝色渐变
# ------------------------
top_colors = plt.cm.Blues(np.linspace(0.8, 0.6, len(total_counts)))
bars_top = ax1.bar(x - width/2, -total_counts.values, width,
                   color=top_colors,
                   edgecolor='black', linewidth=0.8, label='Number of Interactions', alpha=0.9)

# 数据标签
for i, val in enumerate(total_counts.values):
    ax1.text(x[i] - width/2 - 0.10, -val - 50, str(val), ha='center', va='top', fontsize=7, fontweight='bold', color='darkblue')

ax1.set_ylim(-max(total_counts.values)*1.1, 0)  # 反向 y 轴
ax1.set_yticklabels([str(int(abs(y))) for y in ax1.get_yticks()])

# ------------------------
# 下方正柱子：Project Coverage
# 使用绿色渐变
# ------------------------
ax2 = ax1.twinx()
bottom_colors = plt.cm.Greens(np.linspace(0.8, 0.6, len(project_counts)))
bars_bottom = ax2.bar(x + width/2, project_counts.values, width,
                      color=bottom_colors,
                      edgecolor='black', linewidth=0.8, label='Number of Projects by Interaction Type', alpha=0.9)
ax2.tick_params(axis='y', colors='darkgreen')

# 数据标签
for i, val in enumerate(project_counts.values):
    ax2.text(x[i] + width/2 + 0.05, val + 0.2, str(val), ha='center', va='bottom', fontsize=10, fontweight='bold', color='darkgreen')


# x轴标签
ax1.set_xticks(x)
ax1.set_xticklabels(sorted_index, rotation=25, ha='right', fontsize=10)

# 标题
# ax1.set_title("Number of Interactions and Number of Projects by Interaction Type", fontsize=12, fontweight='bold')



# 图例 - 放在图表最上方中央
ax1.legend(loc='upper right', bbox_to_anchor=(0.4, 1.13), ncol=2, fontsize = 9, frameon=True, fancybox=True, shadow=True)
ax2.legend(loc='upper left', bbox_to_anchor=(0.4, 1.13), ncol=2, fontsize = 9, frameon=True, fancybox=True, shadow=True)
# 网格线
ax1.grid(axis='y', linestyle='--', alpha=0.5)

plt.tight_layout()
plt.show()
