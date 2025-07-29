function hexapod_radial_sim_con_sliders()
    % Longitudes de eslabones
    L0 = 86;
    L1 = 74.28;
    L2 = 140.85;
    base_height = 123.83;

    % Puntos reales de montaje
    mount_points = [
        62.77,  90.45,  base_height;
        86,     0,      base_height;
        65.89, -88.21,  base_height;
        -65.89,  88.21,  base_height;
       -86,     0,      base_height;
       -62.77, -90.45,  base_height
    ];

    %% Parámetros iniciales
    d = 40; al = 60; n = 20; w = 1; rs = 0; ra = 0; c = 0;

    %% Figura
    f = figure('Position',[100 100 1200 800]);

    % Ejes para graficar el hexápodo
    ax = axes('Parent',f, 'Position',[0.08 0.35 0.85 0.6]);
    axis(ax, 'equal'); grid on; hold on;
    xlabel('X'); ylabel('Y'); zlabel('Z'); view(3);
    xlim([-250 250]); ylim([-250 250]); zlim([-100 200]);

    %% Sliders y etiquetas
    sl = struct();
    params = {'d', 'al', 'n', 'w', 'rs', 'ra', 'c'};
    mins =   [  0,    0,   0, -1, -pi,  0, -100];
    maxs =   [100,  150,  50,  1,  pi,  1,  100];
    vals =   [ 40,  50,  20, 1,   0,   0,    0];

    for i = 1:length(params)
        name = params{i};
        base_x = 50 + 130*(i-1);
        uicontrol('Style','text','Position',[base_x, 90, 100, 20],...
                  'String',[name ' = ' num2str(vals(i),'%.2f')],'Tag',['label_' name]);
        sl.(name) = uicontrol('Style','slider','Min',mins(i),'Max',maxs(i),'Value',vals(i),...
            'Position',[base_x, 60, 100, 20],...
            'Callback',@(src,~) updateLabel(name, src.Value));
    end

    %% Animación
    k_total = 0:0.05:60*pi;
    while ishandle(f)
        for k = k_total
            if ~ishandle(f), break; end
            % Leer parámetros desde sliders
            d  = get(sl.d,  'Value');
            al = get(sl.al, 'Value');
            n  = get(sl.n,  'Value');
            w  = get(sl.w,  'Value');
            rs = get(sl.rs, 'Value');
            ra = get(sl.ra, 'Value');
            c  = get(sl.c,  'Value');

            % Obtener puntos objetivos de los efectores
            patas = calcularTrayectoria(d, al, n, w, rs, ra, c, k);

            % Dibujar
            cla(ax);
            draw_custom_body_shape(base_height);

            for i = 1:6
                base_pos = mount_points(i,:);
                target = patas{i}';

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
                plot3([P0(1), P1(1)], [P0(2), P1(2)], [P0(3), P1(3)], 'g', 'LineWidth', 2);
                plot3([P1(1), P2(1)], [P1(2), P2(2)], [P1(3), P2(3)], 'b', 'LineWidth', 2);
                plot3([P2(1), P3(1)], [P2(2), P3(2)], [P2(3), P3(3)], 'r', 'LineWidth', 2);
                plot3(P3(1), P3(2), P3(3), 'ko', 'MarkerFaceColor', 'k');
            end

            xlim([-250 250]); ylim([-250 250]); zlim([-100 200]);
            drawnow;
        end
    end
end

%% Función auxiliar para actualizar las etiquetas de sliders
function updateLabel(name, val)
    lab = findobj('Tag',['label_' name]);
    lab.String = [name ' = ' num2str(val, '%.2f')];
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
