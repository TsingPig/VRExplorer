import pandas as pd
import numpy as np

# 读CSV文件
df = pd.read_csv( r'F:\--CodeRepo\--CodeRepo\Paper Reading\____Writing\Data Files\unity_projects_summary.csv')

# 需要统计的列
metrics = ["Scripts", "LOC", "Files", "Scenes", "GameObjects"]

# 计算描述统计指标
def describe_col(col):
    arr = df[col].values
    mean = np.mean(arr)
    var = np.var(arr, ddof=1)  # 样本方差
    mn = np.min(arr)
    q1 = np.percentile(arr, 25)
    median = np.median(arr)
    q3 = np.percentile(arr, 75)
    mx = np.max(arr)
    return mean, var, mn, q1, median, q3, mx

# 输出LaTeX表格头
print(r"\begin{table}[]")
print(r"    \centering")
print(r"    \caption{Statistical Metrics of Dataset Projects}")
print(r"\resizebox{\linewidth}{!}{")
print(r"\begin{tabular}{lrrrrrrr}")
print()
print(r"\toprule")
print(r" Metric &     Mean &      Variance & Min &      Q1 &  Median &       Q3 &    Max \\")
print(r"\midrule")

# 逐列输出
for m in metrics:
    mean, var, mn, q1, median, q3, mx = describe_col(m)
    # 保留两位小数，整数显示无小数
    def fmt(x):
        if isinstance(x, float) and not x.is_integer():
            return f"{x:.2f}"
        else:
            return f"{int(x)}"
    print(f"{m} & {fmt(mean)} & {fmt(var)} & {fmt(mn)} & {fmt(q1)} & {fmt(median)} & {fmt(q3)} & {fmt(mx)} \\\\")

print(r"\bottomrule")
print(r"\end{tabular}")
print(r"}")
print(r"    \label{tab:Statistical Metrics of Dataset Projects}")
print(r"\end{table}")
