close all; clc; clear all;

cpg = [0 0 0 0 0 0 0 0 0 0];


cpgRes = [cpg];
% Parámetros de las oscilaciones (centralizados)
paramOsc.Ao = 100;
paramOsc.Bo = 120;
paramOsc.Co = 1.5;
paramOsc.Do = 2.7;
paramOsc.a = 1;
paramOsc.b = 1;
paramOsc.tau1o = 80;
paramOsc.tau2o = 160;
paramOsc.tau3o = 1500;
paramOsc.tau4o = 1500;

[Q1, Q2, Q3, E, EP, EI, ED, EDaux,LP ,L2P, L3P] = deal(0);
LP = 20;
Eres = [];
Eres2 = [];
for i = 0:25000
    
    [cpg(1),cpg(2),cpg(3),cpg(4),cpg(5),cpg(6),cpg(7),cpg(8),cpg(9),cpg(10) ] ...
    = CPG(cpg(1),cpg(2),cpg(3),cpg(4),cpg(5),cpg(6),cpg(7),cpg(8),cpg(9),cpg(10),1,paramOsc);

    cpgRes = [cpgRes; cpg];
    
    [Q1, Q2, Q3, E,LP , L2P, L3P] = LOCOMOTION( ...
    Q1, Q2, Q3, E,LP , L2P, L3P, ...
    90-3*cpg(7), -5*cpg(6)+10, cpg(9), 1);

    Eres = [Eres;E];
    Eres2 = [Eres2;Q1 Q2 -Q2-90];
end



plot(Eres(1:20000))
figure()
plot(Eres2(1:20000,1:3))



    L1 = 86;
    L2 = 74.28;
    L3 = 140.85;
    
x1 = (((L1+L2*cosd(Eres2(:,2))+L3*(cosd(Eres2(:,2)+Eres2(:,3))))).*cosd(Eres2(:,1)))';

y1 = (((L1+L2*cosd(Eres2(:,2))+L3*(cosd(Eres2(:,2)+Eres2(:,3)))).*sind(Eres2(:,1))))';

% figure()
% plot(X,Y)

x2 = (x1./x1)*25;
y2 = (L2*sind(Eres2(:,2))+L3*sind(Eres2(:,2)+Eres2(:,3)))';

% Crear la figura
figure;
hold on;
axis equal;
grid on;
xlim([min([x1 x2])-1, max([x1 x2])+1]);
ylim([min([y1 y2])-1, max([y1 y2])+1]);

% Dibujar los dos puntos iniciales
h1 = plot(x1(1), y1(1), 'ro', 'MarkerSize', 10, 'MarkerFaceColor', 'r'); % Punto rojo
h2 = plot(x2(1), y2(1), 'bo', 'MarkerSize', 10, 'MarkerFaceColor', 'b'); % Punto azul

% Animar el movimiento de los dos puntos
for i = 1:length(x1)
    set(h1, 'XData', x1(i), 'YData', y1(i));
    set(h2, 'XData', x2(i), 'YData', y2(i));
    pause(0.0001); % Controla la velocidad de la animación
end
