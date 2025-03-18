import tkinter as tk
from tkinter import filedialog
from tkinter import messagebox
from PIL import Image, ImageTk
import numpy as np
from sklearn.cluster import KMeans
import matplotlib.pyplot as plt


# 打开图片文件
def open_image():
    filepath = filedialog.askopenfilename(title = "选择图片", filetypes = [("Image files", "*.jpg;*.png;*.jpeg")])
    if filepath:
        img = Image.open(filepath)

        # 高度固定为400，宽度按比例缩放
        base_height = 400
        aspect_ratio = img.width / img.height
        new_width = int(base_height * aspect_ratio)
        img = img.resize((new_width, base_height))

        img_tk = ImageTk.PhotoImage(img)
        label_image.config(image = img_tk)
        label_image.image = img_tk

        # 提取主题色
        extract_colors(img)


# 提取图片的主要色
def extract_colors(img):
    global palette_frame  # 在使用之前声明 palette_frame 为全局变量
    img = img.convert("RGB")
    img_array = np.array(img)
    img_array = img_array.reshape((img_array.shape[0] * img_array.shape[1], 3))

    # 使用KMeans聚类算法
    kmeans = KMeans(n_clusters = 6)
    kmeans.fit(img_array)
    colors = kmeans.cluster_centers_.astype(int)

    # 更新颜色调色板和显示的RGB, Hex值
    update_palette(colors)


def update_palette(colors):
    global palette_frame
    palette_frame.pack_forget()  # 清空旧的调色板

    palette_frame = tk.Frame(window)
    palette_frame.pack(pady = 10)

    for color in colors:
        hex_code = rgb_to_hex(color)
        rgb_code = f"RGB: {tuple(color)}"

        # 创建颜色块和相应的RGB值显示
        frame = tk.Frame(palette_frame)
        frame.pack(pady = 2, fill = "x", padx = 10)

        color_label = tk.Label(frame, bg = hex_code, width = 10, height = 2)
        color_label.pack(side = "left")

        hex_label = tk.Label(frame, text = f"{hex_code}  {rgb_code}", width = 40, anchor = "w")
        hex_label.pack(side = "left")


def rgb_to_hex(rgb):
    return '#{:02x}{:02x}{:02x}'.format(rgb[0], rgb[1], rgb[2])


# 创建GUI窗口
window = tk.Tk()
window.title("颜色提取器")

# 显示图片的Label
label_image = tk.Label(window)
label_image.pack(pady = 20)

# 打开图片按钮
btn_open = tk.Button(window, text = "打开图片", command = open_image)
btn_open.pack()

# 颜色调色板Frame
palette_frame = tk.Frame(window)  # 初始化 palette_frame
palette_frame.pack(pady = 10)

window.mainloop()
