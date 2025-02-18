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

plt.figure(figsize=(18, 12))

# Loop through all combinations of MoveSpeed and TurnSpeed
for idx, (move, turn) in enumerate(param_combinations, 1):
    plt.subplot(3, 1, idx)

    # Filter data for current parameter combination
    subset = df[(df['MoveSpeed'] == move) & (df['TurnSpeed'] == turn)]

    # Plot coverage data for each model
    for model, color in zip(['VRExplorer', 'VRGuide'], ['#1f77b4', '#ff7f0e']):
        model_data = subset[subset['Model'] == model]


        # Plot Code Line Coverage
        line_coverage_plot, = plt.plot(model_data['Time'], model_data['Code Line Coverage'],
                                       color = color, linestyle = '-', marker = 'o', markersize = 4, label = f'{model} Line Coverage')

        # Add data label for the initial point of Code Line Coverage
        plt.text(model_data['Time'].iloc[0], model_data['Code Line Coverage'].iloc[0],
                 f"{model_data['Code Line Coverage'].iloc[0]:.2f}%", fontsize = 10,
                 verticalalignment = 'bottom', horizontalalignment = 'right')

        # Plot Interactable Coverage
        interactable_coverage_plot, = plt.plot(model_data['Time'], model_data['InteractableCoverage'],
                                               color = color, linestyle = '--', marker = 'x', markersize = 4, label = f'{model} Interactable Coverage')

        # Add data label for the final (convergent) point of Interactable Coverage
        plt.text(model_data['Time'].iloc[-1], model_data['InteractableCoverage'].iloc[-1],
                 f"{model_data['InteractableCoverage'].iloc[-1]:.2f}%", color=color, fontsize=10,
                 verticalalignment='bottom', horizontalalignment='right')

        # Add data label for the final (convergent) point of Code Line Coverage
        plt.text(model_data['Time'].iloc[-1], model_data['Code Line Coverage'].iloc[-1],
                 f"{model_data['Code Line Coverage'].iloc[-1]:.2f}%", color=color, fontsize=10,
                 verticalalignment='bottom', horizontalalignment='right')


    # Title and labels for clarity
    plt.title(f'Model Performance: Move Speed = {move} m/s, Turn Speed = {turn} deg/s', fontsize=14)
    plt.xlabel('Time (s)', fontsize=12)
    plt.ylabel('Coverage (%)', fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6)
    #plt.legend(loc='lower right', fontsize=10, bbox_to_anchor=(1, 1))
    plt.legend(loc='lower right', fontsize=10)


plt.tight_layout()
plt.savefig('Group1_Model_Comparisons.png', dpi=300, bbox_inches='tight')

# 3. Group 2: Model Performance Across Parameters ----------------------
plt.figure(figsize=(18, 10))

# VRExplorer plot
plt.subplot(2, 1, 1)
for (move, turn), color in zip(param_combinations, ['#1f77b4', '#2ca02c', '#d62728']):
    subset = df[(df['Model'] == 'VRExplorer') &
                (df['MoveSpeed'] == move) & (df['TurnSpeed'] == turn)]

    # Plot Code Line Coverage
    plt.plot(subset['Time'], subset['Code Line Coverage'],
             color=color, linestyle='-', marker='o', markersize=5, label=f'Move {move} m/s, Turn {turn} deg/s Line')

    # Plot Interactable Coverage
    plt.plot(subset['Time'], subset['InteractableCoverage'],
             color=color, linestyle='--', marker='x', markersize=5, label=f'Move {move} m/s, Turn {turn} deg/s Interactable')

plt.title('VRExplorer Performance: Parameter Comparisons', fontsize=16)
plt.xlabel('Time (s)', fontsize=12)
plt.ylabel('Coverage (%)', fontsize=12)
plt.grid(True, linestyle='--', alpha=0.6)
plt.legend(loc='upper left', fontsize=10)

# VRGuide plot
plt.subplot(2, 1, 2)
for (move, turn), color in zip(param_combinations, ['#1f77b4', '#2ca02c', '#d62728']):
    subset = df[(df['Model'] == 'VRGuide') &
                (df['MoveSpeed'] == move) & (df['TurnSpeed'] == turn)]

    # Plot Code Line Coverage
    plt.plot(subset['Time'], subset['Code Line Coverage'],
             color=color, linestyle='-', marker='o', markersize=5, label=f'Move {move} m/s, Turn {turn} deg/s Line')

    # Plot Interactable Coverage
    plt.plot(subset['Time'], subset['InteractableCoverage'],
             color=color, linestyle='--', marker='x', markersize=5, label=f'Move {move} m/s, Turn {turn} deg/s Interactable')

plt.title('VRGuide Performance: Parameter Comparisons', fontsize=16)
plt.xlabel('Time (s)', fontsize=12)
plt.ylabel('Coverage (%)', fontsize=12)
plt.grid(True, linestyle='--', alpha=0.6)
plt.legend(loc='upper left', fontsize=10)

plt.tight_layout()
plt.savefig('Group2_Parameter_Comparisons.png', dpi=300, bbox_inches='tight')
plt.show()
