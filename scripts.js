// script.js - Butrul Studio
document.addEventListener('DOMContentLoaded', () => {
    const titleElement = document.getElementById('main-title');
    const tagline = document.querySelector('.tagline');
    const btn = document.querySelector('.primary-btn');
    
    // El texto que queremos escribir
    const textToWrite = "¡Bienvenido a Butrul Studio!"; 
    const typingSpeed = 100; // Velocidad de escritura en milisegundos
    let i = 0;

    // ICONO: Lo añadimos al inicio para que el texto se escriba después de él.
    const initialContent = '<i class="fas fa-hand-fist"></i> '; 
    
    // Inicializamos el H1 solo con el icono.
    titleElement.innerHTML = initialContent; 

    // Configura transiciones CSS explícitas para un fade-in suave
    tagline.style.transition = 'opacity 1s ease-out, transform 1s ease-out';
    btn.style.transition = 'opacity 1s ease-out 0.5s, transform 1s ease-out 0.5s'; // Retraso en el botón
    
    // 1. Ocultar los elementos inicialmente para la animación 'fade-in'
    tagline.style.opacity = '0';
    tagline.style.transform = 'translateY(20px)';
    btn.style.opacity = '0';
    btn.style.transform = 'translateY(20px)';

    // Función para escribir el texto
    function typeWriter() {
        if (i < textToWrite.length) {
            // Añade el nuevo carácter conservando el icono inicial
            titleElement.innerHTML = initialContent + textToWrite.charAt(i);
            i++;
            // Llama a la función de nuevo con un pequeño retraso
            setTimeout(typeWriter, typingSpeed); 
        } else {
            // Cuando termina, elimina el cursor CSS (borde derecho)
            titleElement.style.borderRight = 'none';
            
            // 2. Iniciar la animación 'fade-in' para la tagline y el botón
            const delayBeforeFade = 500;
            
            setTimeout(() => {
                // Muestra los elementos con transición
                tagline.style.opacity = '1';
                tagline.style.transform = 'translateY(0)';
                
                btn.style.opacity = '1';
                btn.style.transform = 'translateY(0)';
            }, delayBeforeFade);
        }
    }

    // Inicia la animación de escritura
    typeWriter();
});
