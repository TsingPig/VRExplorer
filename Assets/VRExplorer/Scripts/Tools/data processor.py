import os
import xml.etree.ElementTree as ET
import matplotlib.pyplot as plt


# 读取文件夹中的所有XML文件
def read_xml_files(folder_path):
    files = [f for f in os.listdir(folder_path) if f.endswith('.xml')]
    return files


# 解析XML文件，提取覆盖率信息
def parse_xml(file_path):
    tree = ET.parse(file_path)
    root = tree.getroot()

    # 提取时间戳
    date = root.attrib['date']

    # 初始化数据结构
    class_data = {
        'date': date,
        'coveredlines': 0,
        'coverablelines': 0,
        'totallines': 0,
        'coveredbranches': 0,
        'totalbranches': 0
    }

    # 遍历所有class节点，获取各项覆盖率
    for assembly in root.findall('assembly'):
        for class_elem in assembly.findall('class'):
            coveredlines = int(class_elem.attrib['coveredlines'])
            coverablelines = int(class_elem.attrib['coverablelines'])
            totallines = int(class_elem.attrib['totallines'])
            coveredbranches = int(class_elem.attrib['coveredbranches'])
            totalbranches = int(class_elem.attrib['totalbranches'])

            # 累加覆盖率数据
            class_data['coveredlines'] += coveredlines
            class_data['coverablelines'] += coverablelines
            class_data['totallines'] += totallines
            class_data['coveredbranches'] += coveredbranches
            class_data['totalbranches'] += totalbranches

    return class_data


# 计算覆盖率指标
def calculate_coverage(class_data):
    line_coverage = (class_data['coveredlines'] / class_data['coverablelines']) if class_data[
                                                                                       'coverablelines'] > 0 else 0
    branch_coverage = (class_data['coveredbranches'] / class_data['totalbranches']) if class_data[
                                                                                           'totalbranches'] > 0 else 0

    return line_coverage, branch_coverage


# 绘制覆盖率图
def plot_coverage(coverage_data, timestamps):
    plt.figure(figsize = (10, 6))

    # 绘制行覆盖率
    plt.plot(timestamps, coverage_data['line_coverage'], label = 'Line Coverage', marker = 'o')

    # 绘制分支覆盖率
    plt.plot(timestamps, coverage_data['branch_coverage'], label = 'Branch Coverage', marker = 'x')

    # 添加数据标注
    for i, txt in enumerate(coverage_data['line_coverage']):
        plt.annotate(f'{txt:.2f}', (
        timestamps[i], coverage_data['line_coverage'][i]), textcoords = "offset points", xytext = (
        0, 10), ha = 'center')

    for i, txt in enumerate(coverage_data['branch_coverage']):
        plt.annotate(f'{txt:.2f}', (
        timestamps[i], coverage_data['branch_coverage'][i]), textcoords = "offset points", xytext = (
        0, -10), ha = 'center')

    plt.xlabel('Timestamp')
    plt.ylabel('Coverage Percentage')
    plt.title('Code Coverage Over Time')
    plt.legend()
    plt.xticks(rotation = 45, ha = 'right')
    plt.tight_layout()
    plt.show()


path = r'D:\--UnityProject\VR\subjects\unity-vr-maze-master\unity-vr-maze-master'


# 主函数
def main():
    folder_path = path + '/CodeCoverage/Report-history'  # 指定XML文件夹路径
    files = read_xml_files(folder_path)

    timestamps = []
    line_coverages = []
    branch_coverages = []

    # 解析每个XML文件
    for file in files:
        file_path = os.path.join(folder_path, file)
        class_data = parse_xml(file_path)
        line_coverage, branch_coverage = calculate_coverage(class_data)

        timestamps.append(class_data['date'])
        line_coverages.append(line_coverage)
        branch_coverages.append(branch_coverage)

        # 打印每个文件的覆盖率数据
        print(f"File: {file}")
        print(f"Date: {class_data['date']}")
        print(f"Line Coverage: {line_coverage * 100:.2f}%")
        print(f"Branch Coverage: {branch_coverage * 100:.2f}%\n")

    # 汇总所有数据
    coverage_data = {
        'line_coverage': line_coverages,
        'branch_coverage': branch_coverages
    }

    # 绘制图表
    plot_coverage(coverage_data, timestamps)


if __name__ == "__main__":
    main()
