    
clc; close all; clear all
% Parámetros del sistema
    L1 = 20;
    L2 = 15;
    L3 = 15;
[Q1, Q2, Q3, E, EP, EI, ED, EDaux,LP ,L2P, L3P] = deal(0);
LP = 20
dt =1;
    Q2=0;
    % Inicialización
    res = [];  % Matriz vacía para resultados
    EI = 0;
    EP = 0;
    ED = 0;
    EDaux = 0;
    
       % Simulación
for i = 1:600
        Q1p = 30* 1;


        Q2p = (Q2*0.35 + 5.5*E)*(i>50) 

        Q3p = -Q2-90;

        % Medidas proyectadas
        LPp = (L1 + L2P) * cosd(Q1);
        Ep = (25 - LP)*(i>50);

        L2Pp = L2 * cosd(Q2);
        
        L3Pp = L3 * cosd(Q2 + Q3);

        
        % Actualización de variables con filtro de 1er orden
        Q1 = Q1 + (dt / 5) * (-Q1 + Q1p);
        Q2 = Q2 + (dt /30) * (-Q2 + tanh(Q2p / 60) * 180);
        Q3 = Q3 + (dt / 5) * (-Q3 + Q3p);
        E = E + (dt / 5) * (-E + Ep);

        LP = LP + (dt / 5) * (-LP + LPp);
        L2P = L2P + (dt / 5) * (-L2P + L2Pp);
        L3P = L3P + (dt / 5) * (-L3P + L3Pp);

        % Acumulación para graficar
        res = [res; E, EP, ED, Q1, Q2, Q3p];
    end

    % Graficar resultados
    figure;
    plot(res, 'LineWidth', 1.5);
    grid on;
    xlabel('Iteraciones');
    ylabel('Valor');
    title('Evolución de variables: E, EI, EP, ED, Q1, Q2');
    legend('E', 'EP', 'ED', 'Q1', 'Q2', 'LP');
