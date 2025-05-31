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

    # 输出处理后的内容（不再做颜色处理）
    with open(output_file, 'w', encoding='utf-8') as file:
        file.write(latex_content)

# 示例使用
input_file = 'input_table.txt'
output_file = 'output_table.txt'
generate_latex_table(input_file, output_file)
