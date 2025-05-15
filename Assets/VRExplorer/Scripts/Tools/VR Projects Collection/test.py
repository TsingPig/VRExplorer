import os
import shutil


def delete_common_files(folder_a, folder_b):
    """
    删除A文件夹中所有在B文件夹中也存在的文件

    参数:
        folder_a (str): 要清理的文件夹路径
        folder_b (str): 包含参考文件的文件夹路径
    """
    # 获取两个文件夹中的所有文件名
    files_a = set(os.listdir(folder_a))
    files_b = set(os.listdir(folder_b))

    # 找出两个文件夹中都存在的文件
    common_files = files_a & files_b

    if not common_files:
        print("没有找到重复文件")
        return

    # 删除A文件夹中的重复文件
    deleted_count = 0
    for filename in common_files:
        file_path = os.path.join(folder_a, filename)
        try:
            if os.path.isfile(file_path):
                os.remove(file_path)
                print(f"已删除: {file_path}")
                deleted_count += 1
            elif os.path.isdir(file_path):
                shutil.rmtree(file_path)
                print(f"已删除目录: {file_path}")
                deleted_count += 1
        except Exception as e:
            print(f"删除 {file_path} 时出错: {e}")

    print(f"\n完成! 共删除了 {deleted_count} 个文件/目录")


if __name__ == "__main__":
    # 输入文件夹路径
    folder_a = input("请输入要清理的文件夹路径(A): ").strip()
    folder_b = input("请输入参考文件夹路径(B): ").strip()

    # 验证路径是否存在
    if not os.path.isdir(folder_a):
        print(f"错误: 文件夹 {folder_a} 不存在")
        exit(1)
    if not os.path.isdir(folder_b):
        print(f"错误: 文件夹 {folder_b} 不存在")
        exit(1)

    # 确认操作
    confirm = input(f"确定要从 {folder_a} 中删除所有在 {folder_b} 中存在的文件吗? (y/n): ").lower()
    if confirm == 'y':
        delete_common_files(folder_a, folder_b)
    else:
        print("操作已取消")