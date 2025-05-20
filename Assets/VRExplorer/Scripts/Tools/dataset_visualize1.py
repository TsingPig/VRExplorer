import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

# 1. 初始化设置 ================================================
plt.rcParams.update({
    'font.family': 'Arial',
    'font.size': 8,
    'axes.titlesize': 10,
    'axes.labelsize': 9,
    'xtick.labelsize': 8,
    'ytick.labelsize': 8,
    'legend.fontsize': 8,
    'figure.dpi': 150,
    'figure.constrained_layout.use': True
})

# 自定义颜色设置
box_color = '#b5e4a4'      # 箱型图颜色
median_color = '#000000'   # 中位数颜色
flier_color = '#FF595E'    # 异常值颜色
mean_color = '#b4faf8'     # 均值颜色
scatter_color = '#FFA600'  # 散点图颜色

# 2. 数据准备 ================================================
file_path = r'F:\--CodeRepo\--CodeRepo\Paper Reading\____Writing\Data Files\unity_projects_summary.csv'
df = pd.read_csv(file_path)
columns_to_plot = ['Scripts', 'LOC', 'Files', 'Scenes']

# 3. 创建图形 ================================================
fig, axes = plt.subplots(1, 4, figsize=(6, 3))

# 4. 绘图循环 ================================================
for ax, col in zip(axes, columns_to_plot):
    # 4.1 绘制箱线图
    sns.boxplot(
        y=df[col], ax=ax, width=0.6,
        flierprops=dict(marker='x', markersize=3.2,
                        markerfacecolor=flier_color,
                        markeredgecolor='darkred', alpha=0.7),
        whiskerprops=dict(linewidth=0.8),
        capprops=dict(linewidth=0.8),
        medianprops=dict(color=median_color, linewidth=1.2),
        boxprops=dict(facecolor=box_color, edgecolor=box_color, linewidth=0.8)
    )

    # 4.2 绘制散点图
    sns.stripplot(y=df[col], ax=ax, color=scatter_color,
                  size=3, alpha=0.6, jitter=0.2)

    # 4.3 绘制均值小菱形
    mean_val = df[col].mean()
    ax.scatter(0, mean_val, color=mean_color, marker='D', s=30,
               edgecolor='black', label='Mean')
    # ax.text(0.05, 0.95, f'μ={mean_val:.1f}',
    #         transform=ax.transAxes, ha='left', va='top',
    #         bbox=dict(facecolor='white', alpha=0.8, edgecolor='none', pad=1))

    # 4.4 坐标轴优化
    ax.set_ylabel(col, labelpad=2)
    ax.set_xlabel('')
    ax.grid(True, axis='y', linestyle=':', linewidth=0.5, alpha=0.5)

    # 对LOC和Scripts使用对数刻度
    if col in ['LOC', 'Scripts']:
        ax.set_yscale('log')
        ax.set_ylabel(f'{col} (log scale)')

plt.show()