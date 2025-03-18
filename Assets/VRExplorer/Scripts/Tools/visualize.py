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






def f1():
    plt.figure(figsize = (18, 12))

    # Loop through all combinations of MoveSpeed and TurnSpeed
    for idx, (move, turn) in enumerate(param_combinations, 1):
        plt.subplot(3, 1, idx)

        # Filter data for current parameter combination
        subset = df[(df['MoveSpeed'] == move) & (df['TurnSpeed'] == turn)]

        # Plot coverage data for each model
        for model, color in zip(['VRExplorer', 'VRGuide'], ['#2c91ed', '#f0a73a']):
            model_data = subset[subset['Model'] == model]

            # Plot Code Line Coverage
            plt.plot(model_data['Time'], model_data['Code Line Coverage'],
                                           color = color, linestyle = '-', linewidth = 3, marker = 'o', markersize = 8, label = f'{model} Line Coverage')

            # Add data label for the initial point of Code Line Coverage
            plt.text(model_data['Time'].iloc[0], model_data['Code Line Coverage'].iloc[0],
                     f"{model_data['Code Line Coverage'].iloc[0]:.2f}%", fontsize = 14,
                     verticalalignment = 'bottom', horizontalalignment = 'right')

            # Plot Interactable Coverage
            plt.plot(model_data['Time'], model_data['InteractableCoverage'],
                                                   color = color, linestyle = '--', linewidth = 3, marker = 'x', markersize = 8, label = f'{model} Interactable Coverage')
            x_offset = 2
            # Add data label for the final (convergent) point of Interactable Coverage
            plt.text(model_data['Time'].iloc[-1] + x_offset, model_data['InteractableCoverage'].iloc[-1],
                     f"{model_data['InteractableCoverage'].iloc[-1]:.2f}%", color = color, fontsize = 14,
                         verticalalignment = 'center', horizontalalignment = 'left')

            # Add data label for the final (convergent) point of Code Line Coverage
            plt.text(model_data['Time'].iloc[-1] + x_offset, model_data['Code Line Coverage'].iloc[-1],
                     f"{model_data['Code Line Coverage'].iloc[-1]:.2f}%", color = color, fontsize = 14,
                     verticalalignment = 'center', horizontalalignment = 'left')

            # Draw vertical line from final point to X-axis (Time = 0)
            plt.vlines(model_data['Time'].iloc[-1], 0, model_data['InteractableCoverage'].iloc[-1],
                       color = color, linestyle = ':', linewidth = 3)
            # Add label at the intersection with X-axis
            plt.text(model_data['Time'].iloc[-1], 0, f"Time Cost: {model_data['Time'].iloc[-1]:.2f}s",
                     fontsize = 14, color = color, verticalalignment = 'top', horizontalalignment = 'center')

        # Title and labels for clarity
        plt.title(f'Model Performance: Move Speed = {move} m/s, Turn Speed = {turn} deg/s', fontsize = 14)
        plt.xlabel('Time (s)', fontsize = 12)
        plt.ylabel('Coverage (%)', fontsize = 12)
        plt.grid(True, linestyle = '--', alpha = 0.6)
        plt.legend(loc = 'upper left', fontsize = 10)

    plt.tight_layout()
    plt.show()
f1()