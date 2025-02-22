import pandas as pd
import matplotlib.pyplot as plt
import glob
import os
import numpy as np


# 1. Data Preparation -------------------------------------------------
def parse_folder_name(folder_name):
    """Extract model and parameter information from folder name"""
    parts = folder_name.split('_')
    model = parts[1]  # VRExplorer or VRGuide
    move = int(parts[2].replace('move', ''))
    turn = int(parts[3].replace('turn', ''))
    return model, move, turn


# Read all CSV files
all_data = []
for folder in glob.glob("D:/--UnityProject/VR/subjects/unity-vr-maze-master/Experiment/CodeCoverage_*"):
    if os.path.isdir(folder):
        model, move, turn = parse_folder_name(os.path.basename(folder))
        csv_file = os.path.join(folder, f"{os.path.basename(folder)}_coverage.csv")

        if os.path.exists(csv_file):
            df = pd.read_csv(csv_file)
            df['Model'] = model
            df['MoveSpeed'] = move
            df['TurnSpeed'] = turn
            all_data.append(df)

df = pd.concat(all_data)
# Remove rows with NaN or inf values from the dataframe
df = df.replace([np.inf, -np.inf], np.nan).dropna()

# 2. Group 1: Model Comparisons Across Different Parameters ----------------------------
param_combinations = df[['MoveSpeed', 'TurnSpeed']].drop_duplicates().values



# 3. Group 2: Model Performance Across Parameters ----------------------

def f2():
    plt.figure(figsize=(18, 10))

    # VRExplorer plot
    plt.subplot(2, 1, 1)
    for (move, turn), color in zip(param_combinations, ['#1f77b4', '#2ca02c', '#d62728']):
        subset = df[(df['Model'] == 'VRExplorer') & (df['MoveSpeed'] == move) & (df['TurnSpeed'] == turn)]

        # Plot Code Line Coverage with transparency
        plt.plot(subset['Time'], subset['Code Line Coverage'],
                 color=color, linestyle='-', marker='o', markersize=6, label=f'Move {move} m/s, Turn {turn} deg/s Line', alpha=0.6)

        # Plot Interactable Coverage with transparency
        plt.plot(subset['Time'], subset['InteractableCoverage'],
                 color=color, linestyle='--', marker='x', markersize=6, label=f'Move {move} m/s, Turn {turn} deg/s Interactable', alpha=0.6)

    plt.title('VRExplorer Performance: Parameter Comparisons', fontsize=16)
    plt.xlabel('Time (s)', fontsize=12)
    plt.ylabel('Coverage (%)', fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6)
    plt.legend(loc='upper left', fontsize=12)

    # VRGuide plot
    plt.subplot(2, 1, 2)
    for (move, turn), color in zip(param_combinations, ['#1f77b4', '#2ca02c', '#d62728']):
        subset = df[(df['Model'] == 'VRGuide') & (df['MoveSpeed'] == move) & (df['TurnSpeed'] == turn)]

        # Plot Code Line Coverage with transparency
        plt.plot(subset['Time'], subset['Code Line Coverage'],
                 color=color, linestyle='--',linewidth = 5, markersize=6, label=f'Move {move} m/s, Turn {turn} deg/s Line', alpha=0.6)

        # Plot Interactable Coverage with transparency
        plt.plot(subset['Time'], subset['InteractableCoverage'],
                 color=color, linestyle='--',linewidth = 5, markersize=6, label=f'Move {move} m/s, Turn {turn} deg/s Interactable', alpha=0.6)

    plt.title('VRGuide Performance: Parameter Comparisons', fontsize=16)
    plt.xlabel('Time (s)', fontsize=12)
    plt.ylabel('Coverage (%)', fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6)
    plt.legend(loc='upper left', fontsize=10)

    plt.tight_layout()
    plt.savefig('Group2_Parameter_Comparisons.png', dpi=300, bbox_inches='tight')
    plt.show()

f2()


