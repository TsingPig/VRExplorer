import os
import xml.etree.ElementTree as ET
import csv

# 设置路径，输入数据文件夹路径
input_folder = r"D:\--UnityProject\VR\subjects\unity-vr-maze-master\Experiment"

# 汇总文件路径
summary_csv_filename = "coverage_summary.csv"
summary_csv_filepath = os.path.join(input_folder, summary_csv_filename)

# 打开总汇总CSV文件并写入表头
with open(summary_csv_filepath, mode = 'w', newline = '') as summary_csvfile:
    summary_csv_writer = csv.writer(summary_csvfile)
    summary_csv_writer.writerow(['Folder Name', 'Report Date', 'Code Line Coverage (%)', 'Method Coverage (%)'])

    # 遍历每个CodeCoverage_xxx文件夹
    for folder_name in os.listdir(input_folder):
        folder_path = os.path.join(input_folder, folder_name)
        if os.path.isdir(folder_path) and folder_name.startswith("CodeCoverage"):
            # 初始化CSV文件
            csv_filename = f"{folder_name}_coverage.csv"
            csv_filepath = os.path.join(folder_path, csv_filename)


            # 1. 在遍历每个文件夹时读取 InteractableAndStateCoverageReport.csv
            interactable_csv_filename = "InteractableAndStateCoverageReport.csv"
            interactable_csv_filepath = os.path.join(folder_path, interactable_csv_filename)

            # 判断文件是否存在
            if os.path.exists(interactable_csv_filepath):
                interactable_data = []
                with open(interactable_csv_filepath, mode = 'r', newline = '') as interactable_csvfile:
                    interactable_csv_reader = csv.reader(interactable_csvfile)
                    next(interactable_csv_reader)  # 跳过表头

                    # 提取 'InteractableCoverage' 列的数据
                    for row in interactable_csv_reader:
                        interactable_data.append(row[5])


            # 打开CSV文件并写入表头
            with open(csv_filepath, mode = 'w', newline = '') as csvfile:
                csv_writer = csv.writer(csvfile)
                csv_writer.writerow(['Time', 'Code Line Coverage', 'InteractableCoverage'])

                # 遍历Report-history文件夹中的所有XML文件
                report_history_path = os.path.join(folder_path, 'Report-history')
                i = 0
                for xml_file in os.listdir(report_history_path):
                    if xml_file.endswith(".xml"):
                        xml_filepath = os.path.join(report_history_path, xml_file)

                        # 解析XML文件
                        tree = ET.parse(xml_filepath)
                        root = tree.getroot()

                        # 获取report的日期
                        report_date = root.attrib['date']

                        # 计算整个程序集的覆盖率
                        total_covered_lines = 0
                        total_coverable_lines = 0

                        for assembly in root.findall('assembly'):
                            for class_elem in assembly.findall('class'):
                                covered_lines = int(class_elem.attrib['coveredlines'])
                                coverable_lines = int(class_elem.attrib['coverablelines'])

                                total_covered_lines += covered_lines
                                total_coverable_lines += coverable_lines

                        # 计算覆盖率
                        if total_coverable_lines > 0:
                            coverage = (total_covered_lines / total_coverable_lines) * 100
                        else:
                            coverage = 0.0

                        # 写入数据并附加 'InteractableCoverage'
                        interactable_coverage = interactable_data[i] if i < len(interactable_data) else 'N/A'  # 确保不会越界
                        csv_writer.writerow([i * 5, f"{coverage:.2f}", interactable_coverage])
                        i += 1

                # 处理Summary.xml文件
                summary_file_path = os.path.join(folder_path, 'Report', 'Summary.xml')
                if os.path.exists(summary_file_path):
                    tree = ET.parse(summary_file_path)
                    root = tree.getroot()

                    # 获取Summary中的coverage和methodcoverage
                    coverage = root.find('.//Assembly').attrib['coverage']
                    methodcoverage = root.find('.//Assembly').attrib['methodcoverage']

                    # 写入总汇总CSV文件
                    summary_csv_writer.writerow([folder_name, 'N/A', coverage, methodcoverage])

print("Data conversion and summary completed.")


