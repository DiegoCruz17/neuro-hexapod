clc; clear all; close all;

%% Parámetros iniciales
d = 40;
al = 15;
n = 20;
w = 1;
rs = pi/4;
ra = 0;
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

%% Figura y sliders
f = figure('Position',[100 100 800 600]);
axis equal
grid on
hold on
xlabel('X')
ylabel('Y')
zlabel('Z')
view(3)

% Slider d
slider_d = uicontrol('Style','slider','Min',10,'Max',100,'Value',d,...
                     'Position',[20 20 150 20],'Callback',[]);
uicontrol('Style','text','Position',[20 40 150 20],'String','d');

% Slider al
slider_al = uicontrol('Style','slider','Min',0,'Max',50,'Value',al,...
                     'Position',[200 20 150 20],'Callback',[]);
uicontrol('Style','text','Position',[200 40 150 20],'String','al');

% Slider n
slider_n = uicontrol('Style','slider','Min',0,'Max',50,'Value',n,...
                     'Position',[380 20 150 20],'Callback',[]);
uicontrol('Style','text','Position',[380 40 150 20],'String','n');

% Slider w
slider_w = uicontrol('Style','slider','Min',0.1,'Max',5,'Value',w,...
                     'Position',[560 20 150 20],'Callback',[]);
uicontrol('Style','text','Position',[560 40 150 20],'String','w');

% Slider rs
slider_rs = uicontrol('Style','slider','Min',-pi,'Max',pi,'Value',rs,...
                     'Position',[20 70 150 20],'Callback',[]);
uicontrol('Style','text','Position',[20 90 150 20],'String','rs');

% Slider ra
slider_ra = uicontrol('Style','slider','Min',-pi,'Max',pi,'Value',ra,...
                     'Position',[200 70 150 20],'Callback',[]);
uicontrol('Style','text','Position',[200 90 150 20],'String','ra');

% Slider c
slider_c = uicontrol('Style','slider','Min',-100,'Max',100,'Value',c,...
                     'Position',[380 70 150 20],'Callback',[]);
uicontrol('Style','text','Position',[380 90 150 20],'String','c');

%% Animación
k_total = 0:0.05:60*pi;

for k = k_total
    % Leer hiperparámetros actualizados
    d = get(slider_d,'Value');
    al = get(slider_al,'Value');
    n = get(slider_n,'Value');
    w = get(slider_w,'Value');
    rs = get(slider_rs,'Value');
    ra = get(slider_ra,'Value');
    c = get(slider_c,'Value');

    % Dependientes
    rah = ra*pi/4;
    rahh = -ra*pi/4;

    % Cálculo de trayectorias
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

    % Actualizar gráfico
    cla
    plot3(P1(1,:), P1(2,:), P1(3,:), 'ro', 'MarkerFaceColor', 'r')
    plot3(P2(1,:), P2(2,:), P2(3,:), 'bo', 'MarkerFaceColor', 'b')
    plot3(P3(1,:), P3(2,:), P3(3,:), 'ro', 'MarkerFaceColor', 'r')
    plot3(P4(1,:), P4(2,:), P4(3,:), 'bo', 'MarkerFaceColor', 'b')
    plot3(P5(1,:), P5(2,:), P5(3,:), 'ro', 'MarkerFaceColor', 'r')
    plot3(P6(1,:), P6(2,:), P6(3,:), 'bo', 'MarkerFaceColor', 'b')

    axis equal
    xlim([-150 150])
    ylim([-150 150])
    zlim([-50 50])
    view(3)

    drawnow
end
