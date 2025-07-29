    % Parámetros del sistema
    L1 = 20;
    L2 = 15;
    L3 = 15;
[Q1, Q2, Q3, E, EP, EI, ED, EDaux,LP ,L2P, L3P] = deal(0);
LP = 20
dt =1;
    % Inicialización
    res = [];  % Matriz vacía para resultados
    EI = 0;
    EP = 0;
    ED = 0;
    EDaux = 0;

    % Simulación
    for i = 1:400
        Q1p = 15 * 1;

        % ✅ Dirección de control corregida usando Q2
        %direccion = sign(sind(Q2));  % Porque cos(Q2) disminuye al alejarse de 0°
        Q2p = ( EI + 2*EP) ;

        Q3p = -15 * 1 - 90;

        % Medidas proyectadas
        LPp = (L1 + L2P) * cosd(Q1);
        Ep = 25 - LP;

        L2Pp = L2 * cosd(Q2);
        L3Pp = L3 * cosd(Q2 + Q3);

        % Control PID neuronal
        [EP, EI, ED, EDaux] = PIDneuronal(EP, EI, ED, EDaux, E, 1);

        % Actualización de variables con filtro de 1er orden
        Q1 = Q1 + (dt / 20) * (-Q1 + Q1p);
        Q2 = Q2 + (dt / 20) * (-Q2 + tanh(Q2p / 30) * 180);
        Q3 = Q3 + (dt / 20) * (-Q3 + tanh(Q3p / 60) * 180);
        E = E + (dt / 20) * (-E + Ep);

        LP = LP + (dt / 20) * (-LP + LPp);
        L2P = L2P + (dt / 20) * (-L2P + L2Pp);
        L3P = L3P + (dt / 20) * (-L3P + L3Pp);

        % Acumulación para graficar
        res = [res; E, E, EP, ED, Q1, Q2];
    end

    % Graficar resultados
    figure;
    plot(res, 'LineWidth', 1.5);
    grid on;
    xlabel('Iteraciones');
    ylabel('Valor');
    title('Evolución de variables: E, EI, EP, ED, Q1, Q2');
    legend('E', 'EI', 'EP', 'ED', 'Q1', 'Q2');

    function [EP, EI, ED, EDaux] = PIDneuronal(EP, EI, ED, EDaux, E, dt)
    EPp = E;
    EIp = EI + E;
    [EDaux, ED] = DERIVATOR(EDaux, ED, E, 1);
    EP = EP + (dt / 2) * (-EP + EPp);
    EI = EI + (dt / 2) * (-EI + EIp);
end