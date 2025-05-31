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

    # 定义一个函数来根据数值生成渐变色
    def get_gradient_color(value):
        # 确保百分比在 0 到 100 范围内
        value = max(0, min(value, 100))

        # 定义更柔和的渐变色过渡：
        if value < 60:
            # 从白色到淡绿色
            return f"white!{int(100 - value * 1.5)}!green!70"
        elif value < 75:
            # 从淡绿色到淡黄色
            return f"green!{int((75 - value) * 5)}!yellow!70"
        elif value < 90:
            # 从淡黄色到淡橙色
            return f"yellow!{int((90 - value) * 4)}!orange!70"
        else:
            # 从淡橙色到红色
            return f"orange!{int((100 - value) * 4)}!red!70"

    # 使用正则表达式提取表格中的数值并替换颜色
    def replace_with_color(match):
        value = float(match.group(1))  # 获取数值
        color = get_gradient_color(value)  # 计算颜色
        return f"\\cellcolor{{{color}}} {value:.2f}"

    # 对表格中的 ELOC Coverage, Method Coverage, 和 Interactable Coverage 列进行处理
    # 正则匹配：数值（百分比）所在位置
    pattern = r'(\d+\.\d{2})'  # 匹配类似 66.53 的数值
    latex_content = re.sub(pattern, replace_with_color, latex_content)

    # 输出处理后的 LaTeX 表格内容到文件
    with open(output_file, 'w', encoding='utf-8') as file:
        file.write(latex_content)

# 使用示例
input_file = 'input_table.txt'  # 输入的 LaTeX 文件路径
output_file = 'output_table.txt'  # 输出的 LaTeX 文件路径
generate_latex_table(input_file, output_file)
