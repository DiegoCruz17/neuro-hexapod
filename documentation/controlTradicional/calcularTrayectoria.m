function patas = calcularTrayectoria(d, al, n, w, rs, ra, c, k)
% calcularTrayectoria - Calcula las posiciones de las 6 patas de un hexápodo
%
% Sintaxis:
%   patas = calculaPatas(d, al, n, w, rs, ra, c, k)
%
% Entradas:
%   d   - Amplitud de trayectoria elíptica [mm]
%   al  - Altura de paso [mm]
%   n   - Factor de deformación de la elipse [mm]
%   w   - Relación de frecuencia de las patas internas [-]
%   rs  - Rotación general del cuerpo [rad]
%   ra  - Ángulo de apertura [-]
%   c   - Offset lateral de trayectoria [mm]
%   k   - Parámetro temporal o angular [rad]
%
% Salida:
%   patas - Celda de 6 elementos, cada uno es un vector columna [3x1] con la posición XYZ de cada pata

    % Definición de funciones auxiliares
    fp = @(k,d,n) d*sin(k) + n*sin(k).*cos(k).^2;
    Rz = @(theta) [cos(theta), -sin(theta), 0;
                   sin(theta),  cos(theta), 0;
                   0,           0,          1];

    % Posiciones de origen de las patas (hexápodo en plano XY)
    hb = -60;
    wb = 15;
    P = [
       -130-wb,-170, hb;
       -170-wb,   0, hb;
       -130-wb, 170, hb;
        130+wb,-170, hb;
        170+wb,   0, hb;
        130+wb, 170, hb
    ];

    % Cálculo de rotaciones dependientes
    rah  = ra*pi/4;
    rahh = -ra*pi/4;

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

    % Cálculo de posiciones finales de las patas
    patas = {
        Rz1*BI + P(6,:)';
        Rzc*RI + P(5,:)';
         Rz2*BI + P(4,:)';
        Rz2*RD + P(3,:)';        
        Rzc*BD + P(2,:)';
        Rz1*RD + P(1,:)'
    };
end
