close all; clear; clc;

%% ========= PARTE 1: Red neuronal (tu código) =========
cpg = zeros(1,10);
paramOsc.Ao = 100;
paramOsc.Bo = 120;
paramOsc.Co = 1.5;
paramOsc.Do = 2.7;
paramOsc.a = 1;
paramOsc.b = 1;
paramOsc.tau1o = 8;
paramOsc.tau2o = 16;
paramOsc.tau3o = 150;
paramOsc.tau4o = 150;

Q1 = zeros(1,6); Q2 = zeros(1,6); Q3 = zeros(1,6); E = zeros(1,6); Ei = zeros(1,6);
LP = 20 * ones(1,6); L2P = zeros(1,6); L3P = zeros(1,6);
N = 5000; % Puedes cambiar esto si quieres más pasos
RangoOPQ1_offset = [60, 0, -60, -60, 0, 60]; 
offsets_Q1_geo = [0, 0, 0,180, 180, 180];

Eres2 = zeros(N, 18);
DIR = [0 0 0 0];
FW = 0; BW = 0; TL = 0; TR = 0; L = 0; R = 0; MOV = 0;
go= 1; dt = 1; bk = 0; spinL = 0; spinR = 0; left = 0; right = 0;

D = 2;
T = 100;
for i = 1:N
    [DIR(1), DIR(2), DIR(3), DIR(4), FW, BW, TL, TR, L, R, MOV] = ...
    Estimulos(DIR(1), DIR(2), DIR(3), DIR(4), FW, BW, TL, TR, L, R, MOV, go, bk, spinL, spinR, left, right, dt);

    [cpg(1),cpg(2),cpg(3),cpg(4),cpg(5),cpg(6),cpg(7),cpg(8),cpg(9),cpg(10)] = ...
        CPG(cpg(1),cpg(2),cpg(3),cpg(4),cpg(5),cpg(6),cpg(7),cpg(8),cpg(9),cpg(10), 1, paramOsc);

    % 6 patas                                                       (Q1,    Q2,     Q3,          E,   ,Ei ,LP,     L2P,    L3P,    T,                                     CPGXY,                           CPGZ,        dt)
    [Q1(1), Q2(1), Q3(1), E(1),Ei(1), LP(1), L2P(1), L3P(1)] = LOCOMOTION(Q1(1), Q2(1), Q3(1), E(1),Ei(1), LP(1), L2P(1), L3P(1), T + 3*cpg(6)*DIR(1)*(DIR(3)-DIR(4)), DIR(1)*cpg(6)*D +RangoOPQ1_offset(1),   3*cpg(9) , 1);
    [Q1(3), Q2(3), Q3(3), E(3),Ei(3), LP(3), L2P(3), L3P(3)] = LOCOMOTION(Q1(3), Q2(3), Q3(3), E(3),Ei(3), LP(3), L2P(3), L3P(3), T + 3*cpg(6)*DIR(1)*(DIR(3)+DIR(4)), DIR(1)*cpg(6)*D + RangoOPQ1_offset(3), 3*cpg(9), 1);
    [Q1(5), Q2(5), Q3(5), E(5),Ei(5), LP(5), L2P(5), L3P(5)] = LOCOMOTION(Q1(5), Q2(5), Q3(5), E(5),Ei(5), LP(5), L2P(5), L3P(5), T + 3*cpg(6)*DIR(1)*(DIR(3))         , DIR(2)*cpg(6)*D + RangoOPQ1_offset(5), 3*cpg(9), 1);
    [Q1(2), Q2(2), Q3(2), E(2),Ei(2), LP(2), L2P(2), L3P(2)] = LOCOMOTION(Q1(2), Q2(2), Q3(2), E(2),Ei(2), LP(2), L2P(2), L3P(2), T + 3*cpg(7)*DIR(2)*DIR(3)         , DIR(1)*cpg(7)*D + RangoOPQ1_offset(2), 3*cpg(10), 1);
    [Q1(4), Q2(4), Q3(4), E(4),Ei(4), LP(4), L2P(4), L3P(4)] = LOCOMOTION(Q1(4), Q2(4), Q3(4), E(4),Ei(4), LP(4), L2P(4), L3P(4), T + 3*cpg(7)*DIR(2)*(DIR(3)+DIR(4)), DIR(2)*cpg(7)*D + RangoOPQ1_offset(4), 3*cpg(10), 1);
    [Q1(6), Q2(6), Q3(6), E(6),Ei(6), LP(6), L2P(6), L3P(6)] = LOCOMOTION(Q1(6), Q2(6), Q3(6), E(6),Ei(6), LP(6), L2P(6), L3P(6), T + 3*cpg(7)*DIR(2)*(DIR(3)-DIR(4)), DIR(2)*cpg(7)*D + RangoOPQ1_offset(6), 3*cpg(10), 1);

    Eres2(i,:) = [Q1 Q2 Q3];
end

%% ========= PARTE 2: Simulación del cuerpo 3D =========
L0 = 86; L1 = 74.28; L2 = 140.85; base_height = 123.83;
mount_points = [
    62.77,  90.45,  base_height;
    86,     0,      base_height;
    65.89, -88.21,  base_height;
   -65.89,  88.21,  base_height;
   -86,     0,      base_height;
   -62.77, -90.45,  base_height
];

f = figure('Position',[100 100 1200 800]);
ax = axes('Parent',f, 'Position',[0.08 0.25 0.85 0.7]);
axis(ax, 'equal'); grid on; hold on;
xlabel('X'); ylabel('Y'); zlabel('Z'); view(3);
xlim([-250 250]); ylim([-250 250]); zlim([-100 200]);

trail = cell(6,1); for i = 1:6, trail{i} = []; end

for i = 1:N
    if ~ishandle(f), break; end
    cla(ax); draw_custom_body_shape(base_height);

    for leg = 1:6
        Q1_val = Eres2(i, leg) + offsets_Q1_geo(leg);
        Q2_val = -Eres2(i, leg + 6);
        Q3_val = -Eres2(i, leg + 12);

        P0 = mount_points(leg, :)';
        Rz = rotz(Q1_val); P1 = P0 + Rz * [L0; 0; 0];
        Ry1 = roty(Q2_val); R1 = Rz * Ry1; P2 = P1 + R1 * [L1; 0; 0];
        Ry2 = roty(Q3_val); R2 = R1 * Ry2; P3 = P2 + R2 * [L2; 0; 0];

        plot3([P0(1), P1(1)], [P0(2), P1(2)], [P0(3), P1(3)], 'g', 'LineWidth', 2);
        plot3([P1(1), P2(1)], [P1(2), P2(2)], [P1(3), P2(3)], 'b', 'LineWidth', 2);
        plot3([P2(1), P3(1)], [P2(2), P3(2)], [P2(3), P3(3)], 'r', 'LineWidth', 2);
        plot3(P3(1), P3(2), P3(3), 'ko', 'MarkerFaceColor', 'k');

        trail{leg}(:, end+1) = P3;
        plot3(trail{leg}(1,:), trail{leg}(2,:), trail{leg}(3,:), 'k:', 'LineWidth', 1);
    end
    pause(0.0001); drawnow;
end

%% ========= FUNCIONES AUXILIARES =========
function R = rotz(theta)
    R = [cosd(theta), -sind(theta), 0;
         sind(theta),  cosd(theta), 0;
         0,            0,           1];
end
function R = roty(theta)
    R = [cosd(theta), 0, sind(theta);
         0,           1, 0;
        -sind(theta), 0, cosd(theta)];
end
function draw_custom_body_shape(z)
    shape_xy = [
         52.12,  112.83;
         82.17,   82.78;
         82.17,  -82.78;
         52.12, -112.83;
        -52.12, -112.83;
        -82.17,  -82.78;
        -82.17,   82.78;
        -52.12,  112.83
    ];
    shape_xy = [shape_xy; shape_xy(1,:)];  % cerrar
    fill3(shape_xy(:,1), shape_xy(:,2), z * ones(size(shape_xy,1),1), ...
        [0.8 0.8 0.8], 'EdgeColor', 'k', 'LineWidth', 2);
end
