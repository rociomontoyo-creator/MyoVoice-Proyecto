
clear all; close all; clc;

% Limpiar conexiones previas
if ~isempty(instrfind), fclose(instrfind); delete(instrfind); end; 

% 1. CONFIGURACIÓN NORAXON (Streaming)
url = 'http://192.168.1.120:9220/samples'; 
opts = weboptions('Timeout', 5, 'ContentType', 'json');

% 2. CONEXIÓN UNITY (Puerto 7000)
u = udp('127.0.0.1', 7000, 'LocalPort', 7001);
fopen(u);

% 3. PARÁMETROS DE PROCESADO
% Nota: En tiempo real no podemos usar 'max(f_env)' porque la señal no ha terminado.
umbral_f = 0.2; % Ajustar este valor (ej: 0.05 a 0.2)
umbral_e = 0.3; % Ajustar este valor
modo_anterior = 0;

% Variables para suavizado (envolvente en tiempo real)
buffer_f = [];
buffer_e = [];
ventana_suavizado = 50; % Tamaño de la media móvil

fprintf('Conectado a MyoVoice y esperando datos de MR3...\n');

while true
    try
        % Leer datos en tiempo real de Noraxon
        data = webread(url, opts);
        
        if ~isempty(data.channels)
            % Extraer muestras del Flexor (Canal 1) y Extensor (Canal 2)
            raw_f = mean(data.channels(1).samples);
            raw_e = mean(data.channels(2).samples);
            
            % Procesado: Rectificación (abs) y suavizado simple
            f_env = abs(raw_f); 
            e_env = abs(raw_e);
            
            % Lógica de detección por umbral
            f_act = f_env > umbral_f;
            e_act = e_env > umbral_e;
            
            % Lógica de control MyoVoice
            if f_act && e_act, modo = 3;      % Ambos -> Seleccionar
            elseif f_act, modo = 1;          % Flexor -> Siguiente
            elseif e_act, modo = 2;          % Extensor -> Anterior
            else, modo = 0; 
            end
            
            % Envío a Unity si hay cambio
            if modo ~= modo_anterior && modo ~= 0
                fwrite(u, num2str(modo), 'char');
                fprintf('>>> ACCIÓN: %d (F: %.4f, E: %.4f)\n', modo, f_env, e_env);
                pause(0.8); % Bloqueo de seguridad para evitar dobles clics
            end
            
            modo_anterior = modo;
        end
        
    catch ME
        % Si hay error de conexión, lo ignoramos y seguimos intentando
    end
    
    pause(1); % Frecuencia de muestreo del bucle
end

% Nota: Para salir usa Ctrl+C y luego ejecuta: fclose(u); delete(u);