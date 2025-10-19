# Juegos-para-Web-y-Redes-Sociales
# 1. Introducción
## 1.1 Descripción breve de la historia
ZPain es un shooter en primera persona de supervivencia y defensa por oleadas, diseñado para ser una experiencia intensa y rejugable. Ambientado en una versión post-apocalíptica de la capital de España, Madrid. El juego se distingue por el combate frenético contra oleadas de infectados. El estilo visual es Low Poly/Cartoon, estableciendo un contraste único entre el estilo animado  y el género de supervivencia y terror. 
## 1.2 Descripción breve de la historia y personajes
Un misterioso y rápido brote vírico ha afectado a la gran mayoría de la población española. El jugador asume el rol de un superviviente anónimo atrapado en Madrid. Este superviviente está buscando la forma de escapar de la zona de cuarentena y encontrar el origen del virus, mientras defiende uno de los puntos clave de la ciudad contra oleadas de zombies. Los personajes principales son los zombies que reflejan a la sociedad madrileña anterior a la infección. 
## 1.3 Propósito, público objetivo y plataformas
El propósito central del juego es ofrecer una experiencia de acción-supervivencia rejugable. Este juego se diferencia por fusionar dos estilos de jugabilidad muy famosos y queridos como son la supervivencia en un mundo post-apocalíptico y el combate frenético. Además el estilo animado ofrece un diseño más limpio y reduce la barrera de edad, haciendo que la acción sea accesible a un público más amplio sin recurrir al gore extremo.

Está dirigido a jugadores de PC, consolas y dispositivos móviles, de 12 a 35 años, que disfrutan de la emoción de los juegos de acción. Busca atraer a los fans de los shooters que valoran las habilidades de puntería y la estrategia de preparación entre las distintas oleadas. 
# 2. Monetización
## 2.1 Tipo de modelo de monetización
El modelo de negocio de ZPain será el Buy to Play (B2P), un pago único para tener acceso completo al juego base. Este modelo, muy bien valorado en el mercado indie de PC y consolas,  garantiza que la calidad de la jugabilidad y la experiencia sean la prioridad. 

Para asegurar beneficios a largo plazo, los ingresos secundarios vendrán de expansiones (DLCs), que incorporan nuevo contenido jugable (mapas o armas) y venta de artículos cosméticos, microtransacciones para la compra de skins de personajes y armas, permitiendo la personalización del juego, para aquellos que busquen modificar el juego según sus gustos, sin ofrecer ninguna ventaja competitiva.

La idea de las expansiones de mapas se basa en recrear nuevas zonas icónicas de España. Incorporar ubicaciones fácilmente reconocibles maximiza la inmersión del jugador y refuerza la conexión con el escenario representado.

## 2.2 Tabla de productos y precios

## 2.3 Estudio de mercado
Para la estimación de los costes de nuestros artículos, se ha realizado una pequeña investigación sobre los precios de los juegos indie, así como de sus contenidos adicionales. Además de esto, también nos hemos inspirado directamente en la opinión de la gente, a través de foros de reddit, para saber cual es el precio que los jugadores estarían dispuestos a pagar por un juego de esta categoría.

https://www.reddit.com/r/Steam/comments/1804xt1/lets_talk_about_indie_game_prices

https://www.reddit.com/r/gamedev/comments/jancdd/what_was_your_indie_game_pricing_strategy

# 3. Planificación y costes
## 3.1 El equipo humano
El equipo está compuesto por 5 desarrolladores que cubren tanto la parte artística como la parte técnica de programación necesaria para desarrollar el videojuego.

La distribución del equipo es la siguiente:

Adrián Espínola Gumiel - Diseño y Desarrollo
David Antonio Paz Gullón - Desarrollo
Hugo Orejas Peláez - Diseño y Documentación
Guillermo Hauschild Arencibia- Diseño y Documentación
Diego Gómez Martín - Desarrollo
## 3.2 Estimación temporal del desarrollo
Fase de preproducción: Etapa inicial y fundamental para el  diseño y la planificación del juego. El equipo se centra en definir el concepto y la estructura antes de iniciar el desarrollo. Se elabora de forma detallada el Documento de Diseño de Juego (GDD), se establecen las bases de las mecánicas principales y se define el estilo artístico.  

Fase de producción: durante la producción, el desarrollo pasa a ser la parte más importante. El equipo se encarga de programar la lógica del gameplay, la IA de los enemigos y de crear e integrar la parte artística. En esta fase se presentará un Alpha del videojuego que contará con la base principal de este, aunque todavía necesite un pulido estético y depuración.

Fase de prueba y correcciones: fase dedicada al control de calidad y el balance. Inicialmente, se realizan pruebas internas del juego para identificar y corregir errores de programación y fallos en el rendimiento. Posteriormente, se lanza una versión Beta, que es un juego completo. El objetivo de esta Beta es realizar un Beta-Testing para obtener feedback por parte de la comunidad y detectar aquellos errores que no se detectaron en las pruebas, además de recibir sugerencias que mejoren la jugabilidad y la experiencia de juego.

Lanzamiento: fase final del proyecto. Consiste en ultimar los detalles y preparar todos los elementos para su publicación. Se implementan las últimas correcciones detectadas en la Beta y se implementaran mejoras. Tras esta revisión, se prepara la versión final del juego completo y se publica en las plataformas seleccionadas.
## 3.3 Estimación de costes asociados
Los costes incluyen:

* Licencias de Software: Motor de juego (Unity), software de modelado 3D profesional (3ds MAX, Blender y Substance Painter) y software de diseño 2D (Photoshop).

* Hardware: Estaciones de trabajo adecuadas para desarrollo 3D.

* Marketing y Pruebas (QA): Presupuesto para la promoción inicial y pruebas de juego por parte de terceros.

# 4. Mecánicas y elementos de juego
## 4.1 Descripción detallada del concepto de juego
El concepto se basa en un Ciclo de Supervivencia y Recompensa. En el primer mapa, el jugador se encuentra en la Plaza de Callao y debe sobrevivir a las oleadas de infectados. Cada victoria de oleada otorga recursos y un tiempo de gracia para reponer munición, cambiar armas y planear una nueva estrategia para sobrevivir a la siguiente ronda, antes de que la siguiente oleada aumente exponencialmente la dificultad.
## 4.2 Descripción detallada de las mecánicas de juego
### 4.2.1 Mecánicas del modo de juego
Progresión por Oleadas:

* El juego avanza a través de rondas.
* Cada ronda tiene un número fijo de enemigos a generar, y solo la siguiente oleada comienza una vez que todos los enemigos de la ronda actual han sido eliminados.
* Existe un tiempo de descanso entre cada oleada para que el jugador pueda recargar, curarse o comprar mejoras.

Dificultad Progresiva:

* Escalamiento de Horda: El número de zombies que aparecen en cada oleada aumenta exponencialmente (en un 5% cada ronda), asegurando que el desafío de gestionar la multitud crezca rápidamente.
* Escalamiento de Resistencia: La vida de los zombies aumenta linealmente con cada oleada, haciendo que sea necesario más daño y más disparos para eliminarlos a medida que el juego avanza.

Generación Dinámica de Enemigos (Spawning):

* El sistema spawnea enemigos en puntos de aparición predefinidos en el mapa, seleccionados al azar.
* Existe un límite máximo de enemigos que pueden estar activos simultáneamente en el mapa, asegurando que el rendimiento del juego se mantenga estable.
### 4.2.2 Mecánicas de Movimiento y Control del Jugador
El personaje se controla como un FPS moderno, con capacidades de movimiento que fomentan la evasión y la movilidad.

Movimiento:

El movimiento se basa en un controlador de personaje, permitiendo interacciones físicas estables sin depender de la física pura (rigidbody).
Sprint: El jugador tiene la capacidad de acelerar su movimiento de forma temporal manteniendo una tecla, esencial para escapar de grandes grupos de enemigos.

Control de Cámara (Mouse Look):

Implementa un control de cámara de tres ejes (rotación horizontal del cuerpo y rotación vertical de la mirada), con la sensibilidad ajustable y el cursor bloqueado.
### 4.2.3 Diseño de Armamento
El sistema de armamento del juego permite definir el rol de cada arma a través de la combinación de daño, cadencia, retroceso y capacidad de munición. Este sistema soporta tres arquetipos principales (por el momento) diseñados para cubrir distintos escenarios de combate en la supervivencia por oleadas.
####4.2.3.1 Pistola
La pistola es el arma inicial del jugador y sirve principalmente como un recurso de precisión y fiabilidad.

* Propósito Táctico: Diseñada para ser utilizada en los momentos iniciales del juego y como un arma de emergencia o respaldo cuando las armas principales se quedan sin munición.

* Mecánica de Disparo: fuego semiautomático; un disparo por acción del jugador.

* Manejo: Su principal fortaleza es su bajo retroceso, lo que la convierte en el arma más fácil de controlar y la más precisa para apuntar a la cabeza o a objetivos distantes.

* Gestión de Recursos: Posee el tiempo de recarga más rápido de todas las armas, minimizando la vulnerabilidad del jugador.

* Mejora: Dispara una ráfaga de 3 balas en vez de disparar una única bala por disparo.
#### 4.2.3.2 Fusil de Asalto
El fusil de asalto es el arma con mayor cadencia del arsenal y la elección óptima para el combate sostenido contra grandes hordas.

* Propósito Táctico: Su función es limpiar rápidamente grandes grupos de enemigos, siendo la herramienta principal para gestionar la dificultad creciente de las oleadas avanzadas.

* Mecánica de Disparo: Posee la cadencia de fuego más alta, permitiendo al jugador infligir un Daño Por Segundo (DPS) masivo.

* Manejo: A pesar de su ritmo de disparo frenético, su retroceso no permite al jugador poder disparar de manera prolongada ya que este arma es más inestable.

* Gestión de Recursos: Viene con la mayor capacidad de cargador y reserva de munición, lo que le permite al jugador disparar ininterrumpidamente durante períodos más largos que cualquier otra arma.

* Mejora: Aumenta considerablemente la cadencia al disparar.
#### 4.2.3.3 Escopeta
La escopeta es un arma con mucho daño pero con poca munición, especializada en la defensa de espacios cerrados y la eliminación de amenazas cercanas.

* Propósito Táctico: No es un arma para daño sostenido, sino una herramienta táctica para el control inmediato de multitudes. Es ideal para enfrentarse a zombies que han logrado acercarse demasiado al jugador.

* Mecánica Única (Dispersión): dispara múltiples proyectiles en cada disparo, los cuales se expanden al avanzar, concentrando un gran daño a corta distancia pero perdiendo efectividad rápidamente con la distancia.

* Manejo: Presenta el retroceso más violento, obligando al jugador a reposicionar la mira después de cada disparo.

* Limitaciones: Su cadencia de fuego es lenta y su tiempo de recarga es el más largo, penalizando severamente al jugador por fallar disparos o no gestionar correctamente el momento de la recarga.

* Mejora: Los perdigones que salen del arma al disparar aumenta a 8.
### 4.2.4 Mecánicas de Combate y Armamento
El combate se centra en el uso de un arma de proyectiles y un sistema de salud compuesto.

Disparo Preciso:
* El sistema de disparo utiliza raycasting, lo que significa que los disparos impactan de forma inmediata y precisa donde apunta el centro de la pantalla, sin depender de la velocidad del proyectil.

* Control de cadencia: El jugador está limitado por una cadencia de fuego definida por el arma equipada, impidiendo disparar de forma continua sin pausas.

Salud Compuesta (HP y Armadura):

* Armadura: Actúa como una capa protectora que absorbe el daño antes de que afecte a la vida principal del jugador.
  
* Vida (HP): El valor principal de supervivencia.

Regeneración Condicional:

* La vida del jugador comienza a regenerarse pasivamente a una velocidad constante sólo después de que ha pasado un periodo de tiempo sin recibir daño, incentivando al jugador a buscar cobertura.
  
Retroalimentación Visual de Impacto:
* Cada vez que el jugador impacta a un zombie, se genera un agujero de bala (decal) que se adhiere al cuerpo del enemigo, proporcionando una respuesta visual clara del acierto.
### 4.2.5 Tipos de Zombies
En este juego, encontraremos 3 tipos de zombies principales y luego un mini-boss que aparecerá cada 5 rondas. Como se ha mencionado anteriormente, la vida de los zombies que se detalla a continuación es cambiante, ya que por cada ronda que el jugador sobreviva, la vida de los zombies aumentará un 10%:

* Zombie Normal: Es el tipo de zombi más común y equilibrado. Posee 100 puntos de salud y es capaz de infligir 20 puntos de daño al jugador cada vez que logra golpearlo. Su velocidad de movimiento es estándar, lo que lo convierte en una amenaza constante, aunque predecible. Es el enemigo ideal para que los jugadores principiantes se familiaricen con las mecánicas básicas del combate.

* Zombie Runner: Este zombi es más pequeño y ágil que el normal. Aunque cuenta únicamente con 50 puntos de salud, compensa su fragilidad con una gran velocidad de movimiento que le permite acercarse rápidamente al jugador. A pesar de su tamaño reducido, inflige el mismo daño que el zombi normal (20 puntos por golpe). Su velocidad y movilidad lo convierten en un enemigo peligroso, especialmente en grupo o en espacios reducidos, donde puede sorprender al jugador fácilmente.

* Zombie Tanque: El zombi Tanque representa la fuerza bruta y la resistencia. Es un enemigo enorme y extremadamente lento, pero su capacidad de aguante es impresionante, con 150 puntos de salud. Además, sus ataques son devastadores, infligiendo 35 puntos de daño por golpe. Aunque su lentitud puede parecer una desventaja, su resistencia y poder destructivo lo convierten en un oponente temible, especialmente en combates prolongados o cuando aparece junto a otros tipos de zombis.

* Zombie Mini-Boss: El mini-jefe es una versión mucho más poderosa y amenazante de los zombis comunes. Posee un tamaño considerablemente mayor, una velocidad de desplazamiento alta para su tamaño y una salud de 200 puntos, lo que lo convierte en uno de los enemigos más difíciles de derrotar. Sus ataques son tremendamente peligrosos, causando 50 puntos de daño cada vez que alcanza al jugador. Este tipo de zombi está diseñado para poner a prueba la habilidad, la estrategia y la capacidad de reacción del jugador, siendo un desafío digno de las etapas más avanzadas del juego.
### 4.2.6 Mecánicas de Enemigos e IA Básica
Los enemigos tienen un comportamiento simple de rastreo y ataque.

Persecución Constante:
* Los enemigos rastrean y se dirigen directamente a la posición del jugador en todo momento.

Ataque por Contacto/Rango:
* Al alcanzar un rango de distancia específico, los enemigos detienen su movimiento e inician una rutina de ataque con un tiempo de enfriamiento.

Detección de Muerte:
* Al recibir suficiente daño, el enemigo se desactiva inmediatamente (deja de moverse y atacar) y desaparece por completo un breve tiempo después.
### 4.2.7 Power-Ups
Los Power-Ups son mejoras que facilitan la supervivencia a lo largo de las rondas, adquiribles en la tienda.

* Bocata de calamares: es un consumible que se adquiere en la tienda. Su función principal es restaurar el blindaje, restaura 100 puntos de vida a la capa de armadura del jugador, sin afectar a la salud base. Su coste es de 1500 puntos.

* Bebida energética: es un consumible que se adquiere en la tienda. Tras su consumición, el jugador recibe un aumento significativo en la velocidad de movimiento. Su coste es de 2000 puntos.
  
* Patatas bravas: es un consumible que se adquiere en la tienda. Esta mejora incrementa la velocidad de recarga de todas las armas del arsenal del jugador. Su coste es de 2500 puntos.

* Schpeppes: es un consumible que se adquiere en la tienda. Tras su compra y uso, otorga un aumento significativo en el daño por disparo para todas las armas equipadas. Su coste es de 3000 puntos.
### 4.2.8 Mecánicas de Economía
El juego implementa una economía básica para recompensar al jugador y proporcionarle mejoras.

Puntuación como Moneda:
* La puntuación funciona como la moneda del juego.
* Se ganan Puntos al eliminar enemigos, con una cantidad variable en función del tipo de enemigo.

Sistema de Compra:
* Zonas de interacción: El jugador debe acercarse a un área o prop específico para activar la opción de compra.
* Compra directa: Permite gastar la moneda mediante una interacción de tecla para obtener mejoras.
* Compra de blindaje: La mejora actualmente implementada permite recargar la armadura a un precio fijo (1500 puntos).

Mensajes de Feedback:
* El sistema de compra proporciona mensajes de la interfaz de usuario para indicar el costo, la interacción y si la compra ha fallado (por falta de puntos o si el power-up ya está al máximo).
## 4.3 Controles
El movimiento y acciones del personaje sigue el esquema universal de los shooter en primera persona para garantizar una respuesta rápida del jugador:

* Movimiento: el jugador puede utilizar las flechas de dirección o las teclas W, A, S y D para el movimiento frontal, retroceso y lateral.
* El jugador puede mover su cámara con el movimiento del ratón y puede disparar al hacer click izquierdo. 
* Correr: al mantener pulsada la tecla Shift, el jugador activará la carrera, para moverse rápidamente y escapar de los grupos de infectados.
* Salto: la Barra Espaciadora  permite saltar obstáculos del entorno.
* Interactuar y comprar: la tecla F es la acción principal para interactuar con las tiendas y con los objetos del mapa. 
## 4.4 Niveles y misiones
El juego principal cuenta con un nivel inicial que se desarrolla en un entorno semi-abierto. Este mapa representa la zona de Callao, destacando los puntos importantes haciendo que el jugador reconozca la zona. Todo con un estilo post-apocalíptico.

La misión principal del juego es la supervivencia ilimitada, un modo rejugable que desafía al jugador a  mantenerse vivo y maximizar su puntuación. El juego no posee un final fijo, sino que la dificultad aumenta de forma dinámica con el paso de las rondas. 

De cara al futuro, la estrategia de contenido se basa en la expansión de nuevos mapas de ubicaciones icónicas de España. Estos futuros niveles se diseñarán para ofrecer nuevos desafíos, reforzar la conexión y el atractivo del juego con la ciudad.

# 5. Transfondo
## 5.1 Descripción detallada de la historia y la trama
Año 2034, España atraviesa una de las crisis más oscuras de su historia moderna. Lo que comenzó como un brote viral localizado en los suburbios de Madrid se expandió con una velocidad devastadora, convirtiendo a la mayoría de la población en criaturas hambrientas y sin conciencia. Las autoridades intentaron contener la situación estableciendo una zona de cuarentena alrededor de la capital, pero la cadena de mando colapsó en cuestión de días. 

Las calles del centro, antes repletas de vida, cultura y turistas, se han convertido en un campo de batalla improvisado. Los pocos supervivientes que quedan se comunican por canales clandestinos, compartiendo información, suministros y falsas esperanzas de rescate. Los rumores hablan de un laboratorio oculto bajo la ciudad, responsable del origen del virus, y de un grupo militar que busca eliminar toda evidencia antes de que el mundo descubra la verdad.
El jugador encarna a uno de esos supervivientes, una persona corriente atrapada en un entorno que ya no reconoce. Sin aliados fiables, sin recursos estables y rodeado por una horda que alguna vez fue su gente, su único objetivo es sobrevivir el tiempo suficiente para escapar de la cuarentena… o descubrir lo que realmente ocurrió en Madrid.
## 5.2 Personajes 
### 5.2.1 Superviviente (personaje principal)
Descripción principal: El jugador encarna a un superviviente anónimo atrapado en el centro de Madrid tras el brote vírico. No se trata de un soldado ni un héroe preparado, sino de una persona corriente que ha tenido que adaptarse a un entorno hostil. Su identidad ambigua permite que el jugador se proyecte en él, reforzando la inmersión y la sensación de vulnerabilidad

Apariencia: El personaje presenta un aspecto urbano y descuidado, reflejo de su vida en medio del caos. Viste una camiseta de un equipo de fútbol de la capital, un pantalón de chándal y unas zapatillas básicas, prendas que muestran desgaste por el uso y la supervivencia diaria. Lleva además una mochila promocional de un gimnasio local, en la que guarda sus pocos suministros. Es calvo y luce una barba blanca desaliñada, lo que acentúa su apariencia agotada y su pasado como ciudadano común atrapado en una situación límite.
### 5.2.2 Los zombies
Los zombies representan a la sociedad madrileña previa a la infección, convertida en una masa grotesca y deshumanizada. Cada tipo de zombie conserva rasgos característicos de su vida anterior, lo que aporta variedad visual, mecánica y narrativa a las oleadas. A medida que aumentan las oleadas, los zombies aumentan en número y se vuelven más resistentes.
## 5.3 Objetos
### 5.3.1 Armas
Las armas del juego se comercializan públicamente bajo la apariencia del top manta en Madrid. En la realidad del mundo del juego son traficantes clandestinos los que han reunido y distribuido ese armamento tras apropiárselo por medios ilegales, manteniendo los puestos como tapadera para operaciones de trueque y venta furtiva.

* Glock: La P9 fue diseñada como arma de dotación estándar para cuerpos de seguridad madrileños antes del colapso. Con el paso del tiempo, muchas de ellas quedaron esparcidas por los distritos centrales tras los primeros disturbios. Su fiabilidad, facilidad de mantenimiento y abundancia de munición la convirtieron en el arma más común entre los supervivientes. Es el arma con la que el jugador inicia su travesía por las calles de Madrid, símbolo de supervivencia y de los primeros días del caos.

* AK-47: Los fusiles de asalto llegaron a las manos de los supervivientes a través del mercado negro improvisado en las bocas de metro y los soportales de Callao. Algunos proceden de cargamentos militares desviados, otros de convoyes de seguridad saqueados. En el juego, pueden conseguirse comparándolos a los vendedores del mercado ilegal, que ocultan sus armas bajo mantas polvorientas junto a objetos de contrabando. Estos comerciantes, antiguos contrabandistas reconvertidos en traficantes de supervivencia, ofrecen el fusil como una pieza cara y codiciada por quienes buscan dominar las oleadas más duras.

* SPAS-12: Las escopetas llegaron desde viviendas rurales, tiendas de caza y pequeños comercios del extrarradio: gracias a intercambios y aprovisionamientos locales, muchas unidades fueron adaptadas y revendidas; los vendedores ambulantes las ofrecen como piezas tácticas, ocultas entre objetos cotidianos para no llamar la atención.

### 5.3.2 Power-Ups
* Bocata de calamares: Por 1500 puntos, el humano podrá adquirir uno de los bocata de calamares más sabrosos del mundo, como son los que se preparan en la Plaza Mayor (o eso dicen). Se comenta que te otorga un blindaje capaz de duplicar tu salud.

* Bebida energética: Dicen que es muy poco sano por la cantidad de azúcar y cafeína que tiene, pero es agua bendita para poder correr de los zombies que quieren comerte. Por 2000 puntos, deja atrás a todos los enemigos.

* Patatas bravas: Producto madrileño realizado con salsa brava del MercaRoña, pica tanto que te permite recargar tus armas  mucho más rápido.

* Tónica Schpeppes: Dicen que es solo una tónica, pero su sabor metálico enfurece al jugador, canalizando esa ira directamente en las balas. Por 3000 puntos haz que tus disparos a los zombies hagan más daño
## 5.4 Entornos y lugares
El juego se desarrolla en un Madrid postapocalíptico completamente tomado por los zombies. Las calles, plazas y edificios emblemáticos de la capital se han convertido en zonas de combate y supervivencia, donde cada rincón cuenta una historia del colapso de la ciudad. La ambientación combina el caos urbano con una sensación constante de aislamiento, reforzando la atmósfera de peligro y desesperación. El jugador recorrerá distintos sectores de Madrid en busca de refugio, suministros y respuestas sobre el origen del virus.

El primer escenario del juego, “Calla o Muere”, es una reinterpretación oscura y destruida de la Plaza de Callao, uno de los puntos más reconocibles del centro de Madrid. Lo que antes era una zona comercial y turística repleta de vida, ahora está en ruinas: los cines, tiendas y cafeterías están saqueados, las calles cubiertas de escombros y los edificios parcialmente incinerados.

# 6. Arte
## 6.1 Estética general del juego
La estética del juego se define por un estilo Low Poly estilizado, una elección que garantiza la optimización del rendimiento y la eficiencia de producción. Visualmente,  se emplea una geometría sencilla y funcional para el entorno, la cual genera un contraste con la composición dramática que aportan los personajes y la iluminación atmosférica. Esta iluminación se caracteriza por una profunda oscuridad, estableciendo un importante contraste con las luces puntuales de la ciudad. Este juego de luces y sombras es vital para la experiencia, ya que genera tensión constante basada en el miedo a aquello que permanece oculto y la incertidumbre de los enemigos que se encuentran en la oscuridad.
## 6.2 Apartado visual
Todos los archivos visuales se mantendrán dentro del marco Low Poly para asegurar la coherencia estética y la eficiencia del motor.

Entorno (Mapas Urbanos): El mapa inicial de la Plaza de Callao sirve como ejemplo del diseño de niveles futuros. Cada mapa se construirá de forma sencilla, pero priorizando la representación fiel de las características más icónicas de las zonas urbanas de la ciudad. Esto es crucial para que el jugador reconozca la localización de juego, reforzando la conexión emocional.

Diseño de personajes: los modelos de los infectados tendrán un diseño sencillo con siluetas exageradas y expresivas. Esto no solo facilita la rápida identificación de las diferentes clases de Zombies especiales durante el combate, sino que también refuerza el tono cartoon, contrastando con la seriedad de la situación.
## 6.3 Música
La banda sonora refuerza la atmósfera de terror, tensión y acción frenética mediante una mezcla musical de Horror Industrial y elementos melódicos españoles. Esta combinación le otorga al juego una identidad sonora diferencial. 

La música del juego está compuesta por instrumentos típicos de España, como la guitarra flamenca, castañuelas y percusión tradicional. 

* Música de Menú y Fondo: El juego utiliza composiciones tranquilas y melancólicas cuando el jugador está en los menús. Esta música es más lenta y crea una sensación de miedo e incertidumbre general.

* Música de Partida: Los ritmos se disparan con percusión industrial y ritmos rápidos de guitarra flamenca cuando comienza una partida. Esta música es rápida y tiene la función de aumentar la adrenalina y la acción frenética durante el combate.
## 6.4 Ambiente sonoro
El diseño sonoro actúa como una herramienta fundamental para darle vida al videojuego. Los sonidos de los infectados, como sus gruñidos y pasos. Gracias a esto el jugador puede localizar la ubicación de los enemigos. Además, cada arma cuenta con un sonido característico que permite diferenciar su tipo y calibre, ayudando al jugador a identificar rápidamente qué arma está utilizando. Finalmente, cada vez que el jugador logra puntos o realiza una compra en la tienda,  el juego recompensa de forma audible con sonidos distintivos, reforzando el progreso y la acción de forma inmersiva. 
# 7. Interfaces
## 7.1 Diseños básicos
### 7.1.1 Menú Principal
Logo del Juego: Ubicado en la esquina superior izquierda, es un logo robusto , con el detalle de arañazos de zombie en el fondo.
Botones de Navegación: Los elementos interactivos ("JUGAR", "CRÉDITOS", "OPCIONES") están presentados como tablones de madera desgastada, rotos y atravesados por lo que parecen ser balas o grapas.

* La tipografía de los botones es en mayúsculas, clara y con un color que contrasta bien (un rojo-marrón) con el color de la madera y el fondo oscuro.
  
* El diseño de los botones está ligeramente inclinado, dándole un toque más dinámico que una UI tradicional.
  
Créditos de "BUTRUL STUDIO" que se ubica en la esquina inferior derecha.
### 7.1.2 Menú Pausa/Opciones
Al personal el boton de opciones, o el botón de pausa desde dentro del juego se  redirigirá al jugador a el menú de pausa, en el cual se mostrarán diversas opciones y configuraciones que se podrán modificar para adaptar la experiencia de juego a las comodidades de cada jugador

* Volumen: Permitirá controlar el volumen del juego.
* Sensibilidad: Velocidad a la que la cámara responderá al movimiento del ratón, cuya personalización es clave en los videojuegos en primera persona.
### 7.1.3 Menú Créditos
Un pequeño menú en el que se muestran los nombres de los principales implicados en el desarrollo del videojuego
### 7.1.4 Menú In-Game
Dentro del juego se apreciará una interfaz sencilla, sin demasiados elementos informativos que puedan sobrecargar al jugador e interrumpir la experiencia inmersiva.

* Arma/Mirilla: El arma se situará en la esquina inferior derecha de la pantalla y contará con una mirilla propia de cada arma en el centro de la pantalla.
* Munición: El indicador de munición estará en la esquina inferior derecha de la pantalla.
* Vida/Escudo: Indicador de la salud del jugador y de su escudo actual, situados en la esquina superior izquierda.
* Ronda: las rondas, indicadas en rojo, mostraran cuantas oleadas de zombies ha eliminado el jugador.
* Puntuación: Los zombies otorgarán puntos al ser eliminados, mostrados en la parte media del lado izquierdo de la pantalla.
## 7.2 Diagrama de flujo
Este diagrama de flujo muestra cómo se organiza la navegación de ZPain. El jugador empieza en el Menú de Inicio, donde puede elegir entre comenzar una partida (IN GAME), entrar en Opciones para ajustar la configuración del juego, o visitar los Créditos para ver quiénes participaron en su desarrollo. Durante la partida, el jugador también puede volver al menú o acceder a las opciones si necesita modificar algo sin salir del juego. De esta forma, el flujo refleja una estructura sencilla y cómoda que facilita moverse entre las diferentes partes del juego sin romper la experiencia del jugador.
# 8. Hoja de ruta del desarrollo
## 8.1 Hito 1- Diseño conceptual y prototipo básico
Este hito inicial marca las bases del desarrollo del juego, centrándose en la definición del concepto del juego. El equipo se encargará de completar la documentación, es decir, redactarán el GDD con todas las especificaciones de arte y de gameplay. Además, la actividad principal es crear un prototipo que muestre las mecánicas fundamentales. El objetivo es definir las necesidades del proyecto para así poder planificar y crear un prototipo que muestre la idea inicial del juego.

## 8.2 Hito 2-Implementación de mecánicas
Una vez definidos los aspectos fundamentales del videojuego, se empieza a desarrollar el juego implementando  más mecánicas a parte de las fundamentales. El equipo implementará las 4 clases de armas, el desarrollo completo de la inteligencia artificial de los enemigos, el sistema de puntuación y la economía interna incluyendo la lógica de los power-ups. 

## 8.3 Hito 3-Desarrollo de los aspectos visuales
Este hito se centra en la integración de todos los aspectos visuales y sonoros para transformar el prototipo funcional en un producto que se asemeje a la versión final. Se realizará el modelo del mapa de la Plaza de Callao y de todos los personajes, incluyendo tanto a los zombies básicos y especiales, como al modelo del personaje principal. Por otro lado, se llevará a cabo la composición e implementación de  la banda sonora del juego y de los sonidos ambiente que le otorgarán vida y realismo atmosférico a la partida. 

## 8.4 Hito 4-Pruebas y ajustes finales
La fase final está dedicada al control de calidad y al ajuste exhaustivo de la experiencia de juego, probando todas las mecánicas y aspectos del juego en busca de errores y fallos. Se realizará una fase de Beta-Testing para recoger feedback real de jugadores sobre el gameplay y la jugabilidad. El equipo se centrará en el balance estratégico de la economía, las recompensas y la dificultad,corrigiendo cualquier aspecto que pueda desequilibrar la experiencia del jugador. Las tareas finales incluirán la corrección de bugs, la optimización de rendimiento y el pulido de los aspectos visuales, como la iluminación, la interfaz y efectos visuales.

## 8.5 Fecha de lanzamiento 
El juego final se publicará oficialmente el día (………)

# 9. Post Mortem - Alpha
## 9.1 Lecciones aprendidas
El desarrollo de esta etapa inicial nos ha enseñado varios aspectos importantes que nos ayudarán a seguir con el proyecto:

Acierto con el estilo Low Poly: Elegir este estilo fue una gran decisión. No solo hizo que el juego se vea bien, sino que es muy práctico: nos permite optimizar el rendimiento del juego y facilita la creación de personajes, objetos y escenarios.

Dificultad en la división de tareas: Trabajar en un videojuego es más difícil de gestionar de lo que parece. La mayor lección fue la dificultad para dividir el trabajo y la importancia de la comunicación constante para que las tareas no se superpongan o se dejen a medias. 

El equilibrio lo es todo: Descubrimos que el balanceo de las armas es lo más sensible y difícil de ajustar. Cualquier cambio pequeño desequilibra la experiencia. Tendremos que dedicar más tiempo en realizar las pruebas para asegurarnos de que el juego sea justo y que presente un desafío para el jugador al mismo tiempo.

## 9.2 Trabajo individual
**Adrian**

Para esta primera versión del juego, mi rol se ha centrado principalmente en el desarrollo de las mecánicas principales, incluyendo el sistema de oleadas de zombis, la persecución al jugador, los sistemas de vida y ataque, así como el movimiento del personaje.
Además, me he encargado de montar todas las escenas en Unity, como los menús y la estructura general del juego.
También he trabajado en el modelado de armas que se implementarán en futuras entregas y he contribuido en la elaboración del GDD.

**David**

Para esta primera versión del juego mi rol ha sido principalmente el de desarrollador y programador además de contribuir al gdd, Encargándome de aspectos como la mecánica de las armas y sus mejoras.

**Hugo**

Para esta primera entrega de la versión Alpha del juego, me he encargado de  realizar la documentación del juego (GDD), que sirve como guía principal para poder desarrollar el juego.  Además, he trabajado en el diseño de los concepts, creando tanto los concepts de los distintos zombies que aparecerán en el juego como los concepts de los Power-Ups.

**Guillermo**

En esta primera entrega del juego, me he centrado principalmente en la elaboración del GDD. Además, he empezado con el modelado del escenario, que se implementará en futuras entregas del juego.

**Diego**

