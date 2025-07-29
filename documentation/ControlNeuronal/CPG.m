

function [CPG1, CPG2, CPG3, CPG4,CPG5, CPG6, CPG7,CPG8, CPG9, CPG10 ] = ...
    CPG(CPG1, CPG2, CPG3, CPG4,CPG5, CPG6,CPG7,CPG8,CPG9,CPG10, dt, param)
    % Parámetros obtenidos de la estructura
    Ao = param.Ao; Bo = param.Bo; Co = param.Co; Do = param.Do;
    a = param.a; b = param.b;
    tau1o = param.tau1o; tau2o = param.tau2o; tau3o = param.tau3o; tau4o = param.tau4o;  

    u = 50;   %%%%%%%%%%%%% parametro importante para modificar la marcha
    u2 = 50;
    CPG1 = CPG1 + (dt / tau1o) * (-a * CPG1 + (Ao * (150 - Do * CPG2)^2) / ((Bo + b * CPG3)^2 + (150 - Do * CPG2)^2));
    CPG2 = CPG2 + (dt / tau2o) * (-a * CPG2 + (Ao * (150 - Do * CPG1)^2) / ((Bo + b * CPG4)^2 + (150 - Do * CPG1)^2));
    CPG3 = CPG3 + (dt / tau3o) * (-a * CPG3 + Co * CPG1);
    CPG4 = CPG4 + (dt / tau4o) * (-a * CPG4 + Co * CPG2);
    
    CPG5 = CPG5 + (dt / tau4o) * (-a * CPG5 + 1.01*((1/2) * CPG1+(1/2) * CPG2));
    CPG6 = min(max((CPG6 + (dt / tau4o) * (-a * CPG6 + a * CPG1 -CPG5)),-u),u); %%% x1
    CPG7 = min(max((CPG7 + (dt / tau4o) * (-a * CPG7 + a * CPG2 -CPG5)),-u),u); 
    
    CPG8 = CPG8 + (dt / tau4o) * (-a * CPG8 + 1.01*((1/2) * CPG3 + (1/2) * CPG4));
    CPG9 = min(max((CPG9 + (dt / tau4o*0.5) * (-a * CPG9 + 1.2 * (CPG3 -CPG8))),-u2),u2); %%%%y2
    CPG10 = min(max((CPG10 + (dt / tau4o*0.5) * (-a * CPG10 + 1.2 * (CPG4 -CPG8))),-u2),u2);

    
end