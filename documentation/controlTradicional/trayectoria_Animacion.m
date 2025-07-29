clc; clear all; close all;

d = 40;
al = 15;
k_total = 0:0.05:60*pi; % Paso ajustable para animación
n = 20;
w = -1;
rs = 0;
ra = 0;
rah = ra*pi/4;
rahh = -ra*pi/4;
c = 0;

fp = @(k,d,n) d*sin(k) + n*sin(k).*cos(k).^2;

Rz = @(theta) [cos(theta), -sin(theta), 0;
               sin(theta),  cos(theta), 0;
               0,           0,          1];

P = 2*[40,40,0;
      60,0,0;
      40,-40,0;
      -40,40,0;
      -60,0,0;
      -40,-40,0];

figure
axis equal
grid on
hold on
xlabel('X')
ylabel('Y')
zlabel('Z')

for k = k_total
    
    rd = fp(k,d,n);
    bd = fp(k+pi,d,n);
    ri = fp(w*k,d,n);
    bi = fp(w*k+pi,d,n);

    zp = al*cos(k);
    zd = al*cos(k+pi);

    Rz1 = Rz(rs+rah);
    Rzc = Rz(rs);
    Rz2 = Rz(rs+rahh);

    RD = [c*cos(rd/d); rd; zp];
    BD = [c*cos(bd/d); bd; zd];
    RI = [-c*cos(ri/d); ri; zp];
    BI = [-c*cos(bi/d); bi; zd];

    P1 = Rz1*RD + P(1,:)';
    P6 = Rz1*BI + P(6,:)';
    P2 = Rzc*BD + P(2,:)';
    P5 = Rzc*RI + P(5,:)';
    P3 = Rz2*RD + P(3,:)';
    P4 = Rz2*BI + P(4,:)';

    % Limpiar figura en cada iteración
    cla

    % Dibujar puntos
    plot3(P1(1,:), P1(2,:), P1(3,:), 'ro', 'MarkerFaceColor', 'r')
    plot3(P2(1,:), P2(2,:), P2(3,:), 'bo', 'MarkerFaceColor', 'b')
    plot3(P3(1,:), P3(2,:), P3(3,:), 'ro', 'MarkerFaceColor', 'r')
    plot3(P4(1,:), P4(2,:), P4(3,:), 'bo', 'MarkerFaceColor', 'b')
    plot3(P5(1,:), P5(2,:), P5(3,:), 'ro', 'MarkerFaceColor', 'r')
    plot3(P6(1,:), P6(2,:), P6(3,:), 'bo', 'MarkerFaceColor', 'b')

    % Configuración de vista
    axis equal
    xlim([-150 150])
    ylim([-150 150])
    zlim([-50 50])
     view(3) 

    drawnow
end
