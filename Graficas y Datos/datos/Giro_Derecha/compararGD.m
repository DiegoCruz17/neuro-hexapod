close all; clc; clear all;

% === Nombres de los archivos Excel ===
file1 = 'Giro_derecha_neuronal.xlsx'  
file2 = 'Giro_derecha_cinematica.xlsx'

%maxRows = 1570; % Cambia este valor según el límite de filas que quieras analizar

% === LECTURA DE DATOS ===
data1 = readtable(file1);
data2 = readtable(file2);
maxRows = min([1570, height(data1), height(data2)]);
% Truncar filas
data1 = data1(1:maxRows, :);
data2 = data2(1:maxRows, :);


% === VARIABLES GENERALES (NO TORQUES) ===
varsToPlot = {'PosX', 'PosY', 'PosZ', ...
              'VelX', 'VelY', 'VelZ', ...
              'AngVelX', 'AngVelY', 'AngVelZ', ...
              'LateralDeviation', 'Distance'};

figure('Name', 'Variables Generales');
for i = 1:length(varsToPlot)
    subplot(4, 3, i);
    plot(data1.Time, data1.(varsToPlot{i}), 'r-', ...
         data2.Time, data2.(varsToPlot{i}), 'b--');
    title(varsToPlot{i}, 'Interpreter', 'none');
    xlabel('Time');
    ylabel(varsToPlot{i});
    legend('Neuronal', 'Clásica');
    grid on;
end

% === TORQUES COXA ===
figure('Name', 'Torques Coxa');
for i = 0:5
    subplot(2, 3, i+1);
    varName = sprintf('Leg%d_Coxa_Torque', i);
    plot(data1.Time, data1.(varName), 'r-', ...
         data2.Time, data2.(varName), 'b--');
    title(varName, 'Interpreter', 'none');
    xlabel('Time');
    ylabel('Torque');
    legend('Neuronal', 'Clásica');
    grid on;
end

% === TORQUES FEMUR ===
figure('Name', 'Torques Femur');
for i = 0:5
    subplot(2, 3, i+1);
    varName = sprintf('Leg%d_Femur_Torque', i);
    plot(data1.Time, data1.(varName), 'r-', ...
         data2.Time, data2.(varName), 'b--');
    title(varName, 'Interpreter', 'none');
    xlabel('Time');
    ylabel('Torque');
    legend('Neuronal', 'Clásica');
    grid on;
end

% === TORQUES TIBIA ===
figure('Name', 'Torques Tibia');
for i = 0:5
    subplot(2, 3, i+1);
    varName = sprintf('Leg%d_Tibia_Torque', i);
    plot(data1.Time, data1.(varName), 'r-', ...
         data2.Time, data2.(varName), 'b--');
    title(varName, 'Interpreter', 'none');
    xlabel('Time');
    ylabel('Torque');
    legend('Neuronal', 'Clásica');
    grid on;
end
