import pandas as pd
import matplotlib.pyplot as plt
import glob
import os
import numpy as np


# 1. Data Preparation -------------------------------------------------
# 不再需要移动和旋转速度的提取
def parse_folder_name(folder_name):
    """Extract model information from folder name"""
    parts = folder_name.split('_')
    model = parts[1]  # VRExplorer or VRGuide
    return model


# Read all CSV files
all_data = []
for folder in glob.glob(r"D:\--UnityProject\VR\subjects\UnityVR\_Experiment\CodeCoverage_*"):
    if os.path.isdir(folder):
        model = parse_folder_name(os.path.basename(folder))
        csv_file = os.path.join(folder, f"{os.path.basename(folder)}_coverage.csv")

        if os.path.exists(csv_file):
            df = pd.read_csv(csv_file)
            df['Model'] = model
            all_data.append(df)

df = pd.concat(all_data)
# Remove rows with NaN or inf values from the dataframe
df = df.replace([np.inf, -np.inf], np.nan).dropna()

# 2. Group 1: Model Comparisons Across Different Parameters ----------------------------

# 不再使用 MoveSpeed 和 TurnSpeed 参数进行分组
model_combinations = df['Model'].drop_duplicates().values


# 3. Group 2: Model Performance ----------------------

def f1():
    plt.figure(figsize = (12, 4))

    # Loop through both models (just two models now)
    for model, color in zip(model_combinations, ['#2c91ed', '#f0a73a']):
        # Filter data for current model
        subset = df[df['Model'] == model]

        # Plot ELOC Coverage
        plt.plot(subset['Time'], subset['ELOC Coverage'],
                 label = f'{model} ELOC Coverage', color = color, linewidth = 3, marker = 'o', markersize = 8)

        # Plot Interactable Coverage
        plt.plot(subset['Time'], subset['InteractableCoverage'],
                 label = f'{model} Interactable Coverage', color = color, linestyle = '--', linewidth = 3, marker = 'x', markersize = 8)

        # Add data labels for initial and final points
        plt.text(subset['Time'].iloc[0], subset['ELOC Coverage'].iloc[0],
                 f"{subset['ELOC Coverage'].iloc[0]:.2f}%", fontsize = 14, verticalalignment = 'bottom', horizontalalignment = 'right')
        x_offset = 0.05

        plt.text(subset['Time'].iloc[-1] + x_offset, subset['ELOC Coverage'].iloc[-1],
                 f"{subset['ELOC Coverage'].iloc[-1]:.2f}%", fontsize = 14, color = color,verticalalignment = 'center', horizontalalignment = 'left')

        plt.text(subset['Time'].iloc[-1]+ x_offset, subset['InteractableCoverage'].iloc[-1],
                 f"{subset['InteractableCoverage'].iloc[-1]:.2f}%", fontsize = 14,color = color, verticalalignment = 'center', horizontalalignment = 'left')

        # Draw vertical line from final point to X-axis (Time = 0)
        plt.vlines(subset['Time'].iloc[-1], 0, subset['InteractableCoverage'].iloc[-1],
                   color = color, linestyle = ':', linewidth = 3)


        # Add label at the intersection with X-axis
        plt.text(subset['Time'].iloc[-1], 0, f"{subset['Time'].iloc[-1]:.2f}s",
                 fontsize = 14, color = color, verticalalignment = 'top', horizontalalignment = 'center')
    # Title and labels for clarity
    # plt.title('Model Performance Comparison', fontsize = 16)
    plt.xlabel('Time (s)', fontsize = 14)
    plt.ylabel('Coverage (%)', fontsize = 14)
    plt.grid(True, linestyle = '--', alpha = 0.6)
    plt.legend(loc = 'upper left', fontsize = 12)

    plt.tight_layout()
    plt.show()


f1()
