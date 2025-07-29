clc; clear; close all; 

%% Parámetros iniciales
d = 40; al = 15; n = 20; w = -1; rs = 0; ra = 0; c = 0;

% Trayectoria elíptica
fp = @(k,d,n) d*sin(k) + n*sin(k).*cos(k).^2;
Rz = @(theta) [cos(theta), -sin(theta), 0;
               sin(theta),  cos(theta), 0;
               0,           0,          1];
hb = 0;
% Posiciones de inicio de las 6 patas del hexápodo (desde el centro)
P = [
   -130,-170,   hb;
   -170,   0,   hb;
  -130, 170,   hb;
  130,-170,   hb;
  170,   0,   hb;
  130, 170,   hb

];

%% Figura
f = figure('Position',[100 100 1000 700]);

% Crear ejes con espacio en la parte inferior para sliders
ax = axes('Parent',f, 'Position',[0.08 0.35 0.85 0.6]);  % [left bottom width height]
axis(ax, 'equal'); grid on; hold on;
xlabel('X'); ylabel('Y'); zlabel('Z'); view(3);
xlim([-250 250]); ylim([-250 250]); zlim([-100 100]);

% Sliders y etiquetas
sl = struct();
params = {'d', 'al', 'n', 'w', 'rs', 'ra', 'c'};
mins =   [  0,    0,   0, -1, -pi,  0, -100];
maxs =   [100,  50,  50,  1,  pi,  1,  100];
vals =   [ 40,  15,  20, 1,   0,   0,    0];

for i = 1:length(params)
    name = params{i};
    base_x = 50 + 130*(i-1);
    uicontrol('Style','text','Position',[base_x, 90, 100, 20],...
              'String',[name ' = ' num2str(vals(i),'%.2f')],'Tag',['label_' name]);
    sl.(name) = uicontrol('Style','slider','Min',mins(i),'Max',maxs(i),'Value',vals(i),...
        'Position',[base_x, 60, 100, 20],...
        'Callback',@(src,~) updateLabel(name, src.Value));
end

%% Trayectorias
k_total = 0:0.1:60*pi;
rastros = cell(6,1);
for i = 1:6
    rastros{i} = [];
end

while ishandle(f)
    for k = k_total
        % Leer valores actuales
        d  = get(sl.d,  'Value');
        al = get(sl.al, 'Value');
        n  = get(sl.n,  'Value');
        w  = get(sl.w,  'Value');
        rs = get(sl.rs, 'Value');
        ra = get(sl.ra, 'Value');
        c  = get(sl.c,  'Value');

        rah  = ra*pi/4;
        rahh = -ra*pi/4;

        % Rotaciones
        Rz1 = Rz(rs + rah);
        Rzc = Rz(rs);
        Rz2 = Rz(rs + rahh);

        % Trayectorias
        RD = [ c*cos(fp(k,d,n)/d);
              fp(k,d,n);
              al*cos(k)];
        BD = [ c*cos(fp(k+pi,d,n)/d);
              fp(k+pi,d,n);
              al*cos(k+pi)];
        RI = [-c*cos(fp(w*k,d,n)/d);
              fp(w*k,d,n);
              al*cos(k)];
        BI = [-c*cos(fp(w*k+pi,d,n)/d);
              fp(w*k+pi,d,n);
              al*cos(k+pi)];

         
        % Patas
        patas = {
            Rz1*RD + P(1,:)';
            Rzc*BD + P(2,:)';
            Rz2*RD + P(3,:)';
            Rz2*BI + P(4,:)';
            Rzc*RI + P(5,:)';
            Rz1*BI + P(6,:)'
        };

        % Rastro
        for i = 1:6
            rastros{i} = [rastros{i}, patas{i}];
        end

        % Dibujar
        cla;
        for i = 1:6
            col = 'r';
            if mod(i,2)==0, col='b'; end
            plot3(rastros{i}(1,:), rastros{i}(2,:), rastros{i}(3,:), ...
                  'Color',[0.5 0.5 0.5])
            plot3(patas{i}(1), patas{i}(2), patas{i}(3), 'o', ...
                  'MarkerFaceColor',col, 'MarkerEdgeColor',col)
        end

        xlim([-250 250]); ylim([-250 250]); zlim([-100 100]);
        drawnow
    end
end

function updateLabel(name, val)
    lab = findobj('Tag',['label_' name]);
    lab.String = [name ' = ' num2str(val, '%.2f')];
end
