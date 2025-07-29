



function [CPG1, CPG2, CPG3, CPG4,CPG5, CPG6,CPG7] = CPG(CPG1, CPG2, CPG3, CPG4,CPG5, CPG6,CPG7, dt, param)
    % Parámetros obtenidos de la estructura
    Ao = param.Ao; Bo = param.Bo; Co = param.Co; Do = param.Do;
    a = param.a; b = param.b;
    tau1o = param.tau1o; tau2o = param.tau2o; tau3o = param.tau3o; tau4o = param.tau4o;  
    
    u = 4;   %%%%%%%%%%%%% parametro importante para modificar la marcha

    CPG1 = CPG1 + (dt / tau1o) * (-a * CPG1 + (Ao * (150 - Do * CPG2)^2) / ((Bo + b * CPG3)^2 + (150 - Do * CPG2)^2));
    CPG2 = CPG2 + (dt / tau2o) * (-a * CPG2 + (Ao * (150 - Do * CPG1)^2) / ((Bo + b * CPG4)^2 + (150 - Do * CPG1)^2));
    CPG3 = CPG3 + (dt / tau3o) * (-a * CPG3 + Co * CPG1);
    CPG4 = CPG4 + (dt / tau4o) * (-a * CPG4 + Co * CPG2);
    CPG5 = CPG5 + (dt / tau4o) * (-a * CPG5 + 1.01*((1/2) * CPG1+(1/2) * CPG2));
    CPG6 = min(max((CPG6 + (dt / tau4o) * (-a * CPG6 + a * CPG1 -CPG5)),-u),u);
    CPG7 = min(max((CPG7 + (dt / tau4o) * (-a * CPG7 + a * CPG2 -CPG5)),-u),u);
    
end