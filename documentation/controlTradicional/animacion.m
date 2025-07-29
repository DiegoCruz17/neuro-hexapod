function hexapod_radial_sim()
    % Longitudes de eslabones
    L0 = 86;         % Centro del cuerpo al inicio del hombro
    L1 = 74.28;      % Hombro al inicio del codo (brazo)
    L2 = 140.85;     % Final del codo al efector
    base_height = 123.83;  % Altura del cuerpo al suelo

    % Ángulos de montaje (hacia donde apunta cada pata)
    shoulder_mount_angles = [0, 45, 135, 180, 225, 270];

    % Puntos reales de montaje (desde SolidWorks)
    mount_points = [
        86,     0,      base_height;   % Pata 1
        62.77,  90.45,  base_height;   % Pata 2
       -65.89,  88.21,  base_height;   % Pata 3
       -86,     0,      base_height;   % Pata 4
       -62.77, -90.45,  base_height;   % Pata 5
        65.89, -88.21,  base_height    % Pata 6
    ];

    % Puntos objetivo del efector final (definidos manualmente)
    effector_targets = [
        170,    0,     -100;      % pata 1
        130,  170,     -100;      % pata 2
       -130,  170,     -100;      % pata 3
       -170,   0,      -100;      % pata 4
       -130, -170,     -100;      % pata 5
        130, -170,     -100       % pata 6
    ];


    % GráficaG
    figure; hold on; axis equal; grid on;
    xlabel('X'); ylabel('Y'); zlabel('Z');
    view(3);
    xlim([-250, 250]); ylim([-250, 250]); zlim([-100, 200]);

    % Dibuja el cuerpo real con forma octagonal irregular
    draw_custom_body_shape(base_height);

    % Dibujar cada pata
    for i = 1:6
        base_pos = mount_points(i,:);
        target = effector_targets(i,:);

        % Cinemática inversa
        [theta1, theta2, theta3] = inverse_kinematics_hexapod(base_pos, target, L0, L1, L2);

        % Cinemática directa para dibujar
        P0 = base_pos';
        Rz = rotz(theta1);
        P1 = P0 + Rz * [L0; 0; 0];

        Ry1 = roty(theta2);
        R1 = Rz * Ry1;
        P2 = P1 + R1 * [L1; 0; 0];

        Ry2 = roty(theta3);
        R2 = R1 * Ry2;
        P3 = P2 + R2 * [L2; 0; 0];

        % Dibujar segmentos
        plot3([P0(1), P1(1)], [P0(2), P1(2)], [P0(3), P1(3)], 'g', 'LineWidth', 2); % hombro
        plot3([P1(1), P2(1)], [P1(2), P2(2)], [P1(3), P2(3)], 'b', 'LineWidth', 2); % brazo
        plot3([P2(1), P3(1)], [P2(2), P3(2)], [P2(3), P3(3)], 'r', 'LineWidth', 2); % antebrazo
        plot3(P3(1), P3(2), P3(3), 'ko', 'MarkerFaceColor', 'k'); % efector
    end
end

function draw_custom_body_shape(z)
    % Coordenadas reales del cuerpo del hexápodo (medidas de SolidWorks)
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

    % Cerrar el polígono
    shape_xy = [shape_xy; shape_xy(1,:)];

    % Dibujo en 3D con z fijo
    fill3(shape_xy(:,1), shape_xy(:,2), z * ones(size(shape_xy,1),1), [0.8 0.8 0.8], ...
        'EdgeColor', 'k', 'LineWidth', 2);
end


function [theta1, theta2, theta3] = inverse_kinematics_hexapod(base, target, L0, L1, L2)
    dx = target(1) - base(1);
    dy = target(2) - base(2);
    dz = target(3) - base(3);

    theta1 = atan2d(dy, dx);
    Rz = rotz(theta1);
    local = Rz' * [dx; dy; dz];
    xz = [local(1) - L0; local(3)];

    r = norm(xz);
    D = (r^2 - L1^2 - L2^2)/(2*L1*L2);
    D = max(min(D,1),-1); % Clamp
    theta3 = -acosd(D);
    theta2 = -(atan2d(xz(2), xz(1)) - atan2d(L2*sind(theta3), L1 + L2*cosd(theta3)));
end

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
