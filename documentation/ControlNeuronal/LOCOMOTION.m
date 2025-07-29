function [Q1, Q2, Q3, E,Ei, LP, L2P, L3P] = LOCOMOTION(Q1, Q2, Q3, E,Ei, LP, L2P, L3P, T, CPGXY, CPGZ, dt)

    % Parámetros del sistema
    L1 = 86;
    L2 = 74.28;
    L3 = 140.85;

    % Inicialización
    res =[];  % Matriz vacía para resultados
       % Simulación
    for i = 1:200
        Q1p = atan2d(CPGXY,T);

        Q2p =(E+0.34*Ei)*sigmoider(CPGZ)-30*sigmoider(-CPGZ);
        Eip =E+Ei;
        
        Q3p =-Q2-90*sigmoider(CPGZ)+(E+0.2*Ei)*sigmoider(-CPGZ);


        % Medidas proyectadas
        LPp = (L1 + L2P+L3P) * cosd(Q1);
        Ep = (T - LP)*(i>25);

        L2Pp = L2 * cosd(Q2);
        
        L3Pp = L3 * cosd(Q2 + Q3);

        
        % Actualización de variables con filtro de 1er orden
        Q1 = Q1 + (dt / 5) * (-Q1 + Q1p);
        Q2 = Q2 + (dt /5) * (-Q2 + Q2p);
        Q3 = Q3 + (dt / 5) * (-Q3 + Q3p);
        E = E + (dt / 5) * (-E + Ep);
        
        Ei = Ei + (dt / 5) * (-Ei + Eip);

        LP = LP + (dt / 5) * (-LP + LPp);
        L2P = L2P + (dt / 5) * (-L2P + L2Pp);
        L3P = L3P + (dt / 5) * (-L3P + L3Pp);
        %res =[res;E, T, LP, Ei];

    end


end

function y = sigmoider(x)
    % Función sigmoide: y = 1 / (1 + exp(-x))
    y = 1 ./ (1 + exp(-x));
end
