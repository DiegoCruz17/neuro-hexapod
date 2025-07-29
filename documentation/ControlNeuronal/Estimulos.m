function [DIR1, DIR2, DIR3, DIR4, FW, BW, TL, TR, L, R, MOV] = ...
    Estimulos(DIR1, DIR2, DIR3, DIR4, FW, BW, TL, TR, L, R,MOV, ...
              go, bk, spinL, spinR, left, right, dt)

    tau = 10; % constante de tiempo para todas las neuronas

    %% === Neuronas intermedias (salida en [0, 1]) ===
    FW_in = 1*go - 1*BW - 1*TL - 1*TR;
    BW_in = 1*bk - 1*FW - 1*TL - 1*TR;
    TL_in = 1*spinL - 1*BW - 1*FW - 1*TR;
    TR_in = 1*spinR - 1*BW - 1*FW - 1*TL;
    L_in  = 1*left - 1*R;
    R_in  = 1*right - 1*L;
    MOV_in = 5*FW + 5*BW + 5*TL + 5*TR + 5*L + 5*R;
    DIR4_in = 1*TL + 1*TR;

    % Activación Naka-Rushton
    FW_target = naka_rushton(FW_in);
    BW_target = naka_rushton(BW_in);
    TL_target = naka_rushton(TL_in);
    TR_target = naka_rushton(TR_in);
    L_target  = naka_rushton(L_in);
    R_target  = naka_rushton(R_in);
    MOV_target = naka_rushton(MOV_in);
    DIR4_target = naka_rushton(DIR4_in);
    
    
    % Actualización con filtro de primer orden
    FW = FW + (dt / tau) * (-FW + FW_target);
    BW = BW + (dt / tau) * (-BW + BW_target);
    TL = TL + (dt / tau) * (-TL + TL_target);
    TR = TR + (dt / tau) * (-TR + TR_target);
    L  = L + (dt / tau) * (-L + L_target);
    R  = R + (dt / tau) * (-R + R_target);
    MOV = MOV + (dt / tau) * (-MOV + MOV_target);
    DIR4 = DIR4 + (dt / tau) * (-DIR4 + DIR4_target);

    %% === Neuronas de dirección (salida en [-1, 1]) ===
    DIR1_in = 1*FW + 1*TL - 1*BW - 1*TR;
    DIR2_in = 1*FW - 1*TL - 1*BW + 1*TR;
    DIR3_in = 1*R - 1*L;

    % Activación tanh para [-1, 1]
    DIR1_target = tanh(DIR1_in);
    DIR2_target = tanh(DIR2_in);
    DIR3_target = tanh(DIR3_in);

    % Actualización
    DIR1 = DIR1 + (dt / tau) * (-DIR1 + DIR1_target);
    DIR2 = DIR2 + (dt / tau) * (-DIR2 + DIR2_target);
    DIR3 = DIR3 + (dt / tau) * (-DIR3 + DIR3_target);
end

%% === Función de activación Naka-Rushton ===
function y = naka_rushton(x)
    g = 1;         % ganancia máxima
    sigma =0.5;     % semisaturación
    n = 2;         % exponente

    x = max(0, x); % sólo responde a valores positivos
    y = (g * x.^n) ./ (x.^n + sigma^n + eps); % eps evita división por cero
end
