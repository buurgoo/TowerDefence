# AI-agent

agent has 3 options { Wait, Attack, Defend } \
if **Attack** agent also has 3 options send { None, Swordsman, Archer } but this will be used only if we implement the purchase of units \
if **Defend** we choose one avalebel coordinate and put a tower there

$S_0$ [Game Initial $\text{State}_0$] \
$S_1 \rightarrow M_{0}$ [chooses action] \
$A_0$ == [Atack] \
$M_{0.0}$ == [enemy type] 

$S_1$ [Game State] \
$S_2 \rightarrow M_{1}$ [chooses action] \
$A_1$ == [Defence] \
$M_{1.1}$ == [tower coordinates] 

$S_2$ [Game State] \
$S_3 \rightarrow M_{1}$ [chooses action] \
$A_2$ == [Atack] \
$M_{2.2}$ == [enemy type] 

$S_3$ [Game State] \
$S_4 \rightarrow M_{3}$ [chooses action] \
$A_3$ == [Defence] \
$M_{3.3}$ == [tower coordinates] 

- $S_n$ - state n 
- $A_n$ - acktion n 
- $M_{n}$ - moodel choose n
- $M_{n.n}$ - moodel choose atacker n or coordinates

