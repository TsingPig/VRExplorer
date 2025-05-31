import re

def generate_latex_table(input_file, output_file):
    # 读取输入的 LaTeX 文件
    with open(input_file, 'r', encoding='utf-8') as file:
        latex_content = file.read()

    # 检查是否已经有 \resizebox，如果没有则添加
    if r'\resizebox' not in latex_content:
        latex_content = latex_content.replace(
            r'{table}',
            r'{table*}'
        )
        latex_content = latex_content.replace(
            r'\begin{table*}[]',
            r'\begin{table*}[h] \resizebox{\linewidth}{!}{')
        latex_content = latex_content.replace(
            r'\caption',
            r'}\caption')

    # 定义灰度颜色函数（0%为white，100%为gray!100）
    def get_grayscale_color(value):
        # 限制在 0 到 100 之间
        value = max(0, min(value, 100))
        # 计算灰度强度：0% → gray!0（即 white），100% → gray!100（接近黑）
        gray_level = int(value)
        return f"gray!{gray_level}"

    # 用正则替换表格中的百分比数值（保留两位小数）
    def replace_with_grayscale(match):
        value = float(match.group(1))
        color = get_grayscale_color(value)
        return f"\\cellcolor{{{color}}} {value:.2f}"

    # 匹配类似 66.53 的数值
    pattern = r'(\d+\.\d{2})'
    latex_content = re.sub(pattern, replace_with_grayscale, latex_content)

    # 输出处理后的内容
    with open(output_file, 'w', encoding='utf-8') as file:
        file.write(latex_content)

# 示例使用
input_file = 'input_table.txt'
output_file = 'output_table.txt'
generate_latex_table(input_file, output_file)
