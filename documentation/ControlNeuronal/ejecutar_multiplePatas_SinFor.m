close all; clear all; clc

cpg = zeros(1,10);
cpgRes = [cpg];
% Parámetros de las oscilaciones (centralizados)
paramOsc.Ao = 100;
paramOsc.Bo = 120;
paramOsc.Co = 1.5;
paramOsc.Do = 2.7;
paramOsc.a = 1;
paramOsc.b = 1;
paramOsc.tau1o = 80;
paramOsc.tau2o = 160;
paramOsc.tau3o = 1500;
paramOsc.tau4o = 1500;
% Inicialización de variables para 6 patas
Q1 = zeros(1,6);
Q2 = zeros(1,6);
Q3 = zeros(1,6);
E = zeros(1,6);
LP = 20 * ones(1,6);
L2P = zeros(1,6);
L3P = zeros(1,6);
N = 25000;  % número total de iteraciones

RangoOPQ1_offset = [10, 0, -10, 10, 0, -10]; 

offsets_Q1_geo = [0, 0, 0, 180, 180, 180];

DIR = [0 0 0 0];


Eres = zeros(N, 4);      % 6 patas, 1 fila por iteración
Eres2 = zeros(N, 18);    % 3 valores por pata (Q1, Q2, -Q2-90)
dt = 1;

FW = 0; BW = 0; TL = 0; TR = 0; L = 0; R = 0; MOV = 0;

go= 0;
bk = 0;
spinL = 1;
spinR = 0;
left = 0;
right = 0;

for i = 1:N
    
    [DIR(1), DIR(2), DIR(3), DIR(4), FW, BW, TL, TR, L, R, MOV] = ...
    Estimulos(DIR(1), DIR(2), DIR(3), DIR(4), FW, BW, TL, TR, L, R, MOV, ...
              go, bk, spinL, spinR, left, right, dt);
    
    
    [cpg(1),cpg(2),cpg(3),cpg(4),cpg(5),cpg(6),cpg(7),cpg(8),cpg(9),cpg(10)] = ...
        CPG(cpg(1),cpg(2),cpg(3),cpg(4),cpg(5),cpg(6),cpg(7),cpg(8),cpg(9),cpg(10), 1, paramOsc);

    cpgRes(i,:) = cpg;  % también puedes preasignar esto como cpgRes = zeros(N, 10);


% Piernas impares: 1, 3, 5
[Q1(1), Q2(1), Q3(1), E(1), LP(1), L2P(1), L3P(1)] = LOCOMOTION( ...
    Q1(1), Q2(1), Q3(1), E(1), LP(1), L2P(1), L3P(1), ...
    90 + 3*cpg(6)*DIR(1)*(DIR(3)-DIR(4)), ...
    DIR(1)*5*cpg(6) + RangoOPQ1_offset(1), ...
    cpg(9), 1);

[Q1(3), Q2(3), Q3(3), E(3), LP(3), L2P(3), L3P(3)] = LOCOMOTION( ...
    Q1(3), Q2(3), Q3(3), E(3), LP(3), L2P(3), L3P(3), ...
    90 + 3*cpg(6)*DIR(1)*(DIR(3)+DIR(4)), ...
    DIR(1)*5*cpg(6) + RangoOPQ1_offset(3), ...
    cpg(9), 1);

[Q1(5), Q2(5), Q3(5), E(5), LP(5), L2P(5), L3P(5)] = LOCOMOTION( ...
    Q1(5), Q2(5), Q3(5), E(5), LP(5), L2P(5), L3P(5), ...
    90 + 3*cpg(6)*DIR(1)*DIR(3), ...
    DIR(2)*5*cpg(6) + RangoOPQ1_offset(5), ...
    cpg(9), 1);

% Piernas pares: 2, 4, 6
[Q1(2), Q2(2), Q3(2), E(2), LP(2), L2P(2), L3P(2)] = LOCOMOTION( ...
    Q1(2), Q2(2), Q3(2), E(2), LP(2), L2P(2), L3P(2), ...
    90 + 3*cpg(7)*DIR(2)*DIR(3), ...
    DIR(1)*5*cpg(7) + RangoOPQ1_offset(2), ...
    cpg(10), 1);

[Q1(4), Q2(4), Q3(4), E(4), LP(4), L2P(4), L3P(4)] = LOCOMOTION( ...
    Q1(4), Q2(4), Q3(4), E(4), LP(4), L2P(4), L3P(4), ...
    90 + 3*cpg(7)*DIR(2)*(DIR(3)+DIR(4)), ...
    DIR(2)*5*cpg(7) + RangoOPQ1_offset(4), ...
    cpg(10), 1);

[Q1(6), Q2(6), Q3(6), E(6), LP(6), L2P(6), L3P(6)] = LOCOMOTION( ...
    Q1(6), Q2(6), Q3(6), E(6), LP(6), L2P(6), L3P(6), ...
    90 + 3*cpg(7)*DIR(2)*(DIR(3)-DIR(4)), ...
    DIR(2)*5*cpg(7) + RangoOPQ1_offset(6), ...
    cpg(10), 1);

    
    
    Eres(i,:) = [DIR];  % Guarda E por pata
    Eres2(i,:) = [Q1 Q2 -Q2-90];  % Guarda Q1, Q2 y -Q2-90 para todas las patas
end


% Longitudes
L1 = 86;
L2 = 74.28;
L3 = 140.85;

% Número de pasos
N = size(Eres2, 1);

% Inicialización de trayectorias para las 6 patas
x1_all = zeros(6, N);
y1_all = zeros(6, N);
x2_all = zeros(6, N);
y2_all = zeros(6, N);

% Posición base (offset) para cada pata (en X, Y)
radius = 100;  % distancia desde el centro del cuerpo
angle_base = deg2rad([60, 0, 300, 120, 180, 240]);  % 6 patas equiespaciadas

base_pos = [radius * cos(angle_base);   % X
            radius * sin(angle_base)];  % Y

% base_pos es de tamaño (2,6): base_pos(:,leg) te da el [x; y] del soporte de cada pata
for leg = 1:6
    Q1_leg = Eres2(:, leg);         % Q1
    Q2_leg = Eres2(:, leg + 6);     % Q2
    Q3_leg = Eres2(:, leg + 12);    % Q3

    % Coordenadas relativas
    x1 = (L1 + L2.*cosd(Q2_leg) + L3.*cosd(Q2_leg + Q3_leg)) .* cosd(Q1_leg+ offsets_Q1_geo(leg));
    y1 = (L1 + L2.*cosd(Q2_leg) + L3.*cosd(Q2_leg + Q3_leg)) .* sind(Q1_leg+ offsets_Q1_geo(leg));
    x2 = ones(size(x1)) * 25;
    y2 = L2.*sind(Q2_leg) + L3.*sind(Q2_leg + Q3_leg);

    % Desplazamiento con base_pos
    x1 = x1 + base_pos(1, leg);
    y1 = y1 + base_pos(2, leg);
    x2 = x2 + base_pos(1, leg);
    y2 = y2 + base_pos(2, leg);

    % Guardar
    x1_all(leg, :) = x1';
    y1_all(leg, :) = y1';
    x2_all(leg, :) = x2';
    y2_all(leg, :) = y2';
end


% Crear la figura
figure;
hold on;
axis equal;
grid on;

% Definir límites automáticos para todos los puntos
all_x = [x1_all(:); x2_all(:)];
all_y = [y1_all(:); y2_all(:)];
xlim([min(all_x)-10, max(all_x)+10]);
ylim([min(all_y)-10, max(all_y)+10]);

% Dibujar los puntos iniciales
colors = lines(6);  % Diferente color por pata
h1 = gobjects(1,6);
h2 = gobjects(1,6);
for leg = 1:6
    h1(leg) = plot(x1_all(leg,1), y1_all(leg,1), 'o', ...
        'MarkerSize', 8, 'MarkerFaceColor', colors(leg,:), 'Color', colors(leg,:));
    h2(leg) = plot(x2_all(leg,1), y2_all(leg,1), 's', ...
        'MarkerSize', 6, 'MarkerFaceColor', colors(leg,:), 'Color', colors(leg,:));
end

% Animar todas las patas
for i = 1:N
    for leg = 1:6
        set(h1(leg), 'XData', x1_all(leg,i), 'YData', y1_all(leg,i));
        set(h2(leg), 'XData', x2_all(leg,i), 'YData', y2_all(leg,i));
    end
    pause(0.0001);
end



