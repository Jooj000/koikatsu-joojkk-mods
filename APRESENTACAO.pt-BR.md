# Campus Life — apresentação

*English: [OVERVIEW.md](OVERVIEW.md)*

**Um mod de vida universitária para o modo história do Koikatsu.** As garotas da faculdade ganham humor, memória, amizades e mágoas próprias; um diretor planeja o dia de cada uma e decide cada resposta que ela te dá; a conversa vira uma conversa de verdade; e tudo o que você faz com elas vira parte de uma história que o campus inteiro comenta.

Feito por **Joojkk** · versão **3.6.0** · BepInEx 5 / KKAPI · Koikatu (HF Patch) · menu **F7** · interface e falas em inglês.

Junto vêm três mods irmãos: **Campus Saves** (um espaço de save por campus, Ctrl+F6), **School Optimizer** (campus cheio sem travar) e **H Male Wardrobe** (roupa do protagonista no H, F10).

---

## Em números

| | |
|---|---|
| Código | ~47 mil linhas de C# (.NET 3.5), Harmony só com prefix/postfix, tudo em try/catch |
| Falas | 37 arquivos `.dlg` em que cada fala é escrita junto com as opções que respondem a ela, mais as cenas e eventos escritos no código |
| Vozes | 6 tons (gentil, enérgica, fria, provocadora, tímida, formal), cada fala com quatro ou mais versões por tom, nunca a mesma duas vezes seguidas |
| Conquistas | 54 conquistas que sobem por etapas, mais uma escada para cada heroína da história e cada professora |

---

## 1. Conversa

- **Uma tela de conversa nova** com cinco categorias em ícones: **Talk**, **Actions**, **Together**, **Romance** e **Spicy**. As opções saem do que ela sabe, do que ela gosta, de onde vocês estão e do que aconteceu hoje, não de uma lista fixa, e cada opção responde à fala que ela acabou de dizer.
- **Conversa procedural**: assuntos pontuados pelo contexto (o dia dela, vocês dois, você, fofoca, o campus, conversa fiada), fatos que ela sabe e de quem ouviu, opiniões e versões distorcidas. Ela lembra do que você contou e traz de volta dias depois. Toda fala que depende de um gosto ou da chuva tem o lado da garota que não liga para aquilo.
- **A voz dela**: cada fala é escrita no tom dela, moldada pela personalidade, pelo humor e pela relação de vocês; o jogo toca um trecho da própria voz gravada dela que combina com a fala.
- **Toques decididos pelo diretor**: mão, cabeça, bochecha, peito e bunda, mais os lábios e um olhar. Cada toque recebe uma das quatro respostas (abaixo), pesada pelo que você é para ela, o ritmo dela, o humor, a confiança, quem está olhando e quantas vezes você já tocou ali. O mesmo toque no mesmo período tem a mesma resposta, e insistir depois de um não piora.
- **Pedidos com conversa própria**: cada tipo de pedido tem a sua fala, quatro opções escritas para ele e a resposta dela a cada uma.
- **O boato de que ela gosta de você** tem cena própria: ela responde como se sente, e dá para se declarar dali.
- **Conversa com duas garotas**: as duas lado a lado; fale com uma, com a outra ou com as duas. Elas reagem uma à outra, provocam, sentem ciúme, ajudam ou vão embora, e o que dizem uma à outra é sobre coisas verdadeiras naquele momento. Opções que só existem com duas: perguntar de uma para a outra, uma história constrangedora, um bilhete, um convite para as duas, um elogio na frente da outra, um toque escondido.
- **Selos da conversa**: o humor e os gostos dela em ícones redondos sob a barra do H (só depois de você descobrir).
- **Um registro da conversa** inteira, e nada age com uma fala na tela: categorias, opções e toques esperam a caixa fechar.

## 2. O diretor (a IA das garotas)

- **O diretor decide o que as garotas fazem**: a cada período ele atualiza as necessidades delas (companhia, comida, descanso), intenções, pares e boatos, e escolhe as situações que acontecem na sua frente ou longe dela.
- **Quatro respostas** para tudo o que importa (toques, beijo, convites, declaração, encontros, H): **sim**, **sim receoso**, **não receoso** e **não**. Ele lê o máximo de fatores: o que você é para ela, o ritmo dela, as barras, os dias juntos, sua reputação, confiança, humor, H anterior, um beijo recente, outra namorada, um dia perfeito, quantas vezes ela já disse não.
  - Um **sim receoso** deixa culpa: na conversa seguinte ela pede compromisso ou se afasta, e vocês resolvem.
  - Um **não receoso** é quase um sim: ela queria, e diz o que a segurou (personalidade, humor, culpa, sua namorada, cedo demais, já pediu hoje, uma mágoa, se sentir deixada de lado, reputação, ou só não aqui e agora).
- **Ritmo pela personalidade**: garotas reservadas, normais e abertas levam números de dias diferentes (tabela abaixo). O H não exige namoro: depende do contexto. Três opções (Ease: reserved / normal / open) deixam cada ritmo mais fácil ou mais difícil.
- **Términos e traição**: a namorada que descobre outra garota reage pelo humor, raiva, confiança, quantas vezes aconteceu, quem é a outra, até onde foi e se ela viu.
- **Traços do card** (fácil, pervertida, submissa, séria...): quando o card tem todos marcados, ou nenhum, o mod sorteia os que combinam com a personalidade dela em vez de deixar toda garota fácil.
- **Antes do H do jogo** ela diz em voz alta o que o card dela recusa (proteção, beijo, porta dos fundos, toques, brinquedos), com a própria voz. O H do jogo em si não muda.

### Quantos dias até cada marco

Dias bem jogados (3 conversas por dia, respostas boas), com o Ease padrão, média de duas garotas de cada ritmo, sem humor especial:

| Marco | Reservada | Normal | Aberta |
|---|---|---|---|
| Flerte e elogios funcionam | 2 | 2 | 2 |
| Segurar a mão | 3 | 2 | 2 |
| Amiga | 4 | 3 | 3 |
| Namorada | 5 | 4 | 3 |
| Peito e bunda (namorando) | 5 | 4 | — |
| Peito e bunda (só amigos) | — | — | sim receoso 3, sim 4 |
| Beijo (namorando) | 5 | 4 | 3 |
| Beijo (só amigos) | — | — | sim receoso 3, sim 4 |
| H namorando | 7 | 4–5 | sim receoso 2, sim 3 |
| H só amigos | sim receoso 8 | sim receoso 6–7 | sim receoso 2, sim 4 |

- O traço quer dizer que a combinação não foi medida sozinha: a reservada e a normal costumam virar namoradas antes de deixar ir tão longe como amigas.
- **Humor**: flertando adianta cerca de um dia (num primeiro dia perfeito a aberta pode aceitar o H); alegre, cerca de meio dia; sozinha quase não muda; chateada atrasa, a mão para perto do dia 5 e o H um ou dois dias.
- **Ordem das respostas**: em 60 simulações (6 garotas × 5 humores × namorando ou não) a resposta do H nunca voltou atrás: sempre não → não receoso → sim receoso → sim (pulando degraus, nunca descendo).

## 3. Um campus que vive

- **Amizades, rivalidades e romances entre as garotas** (queda, conversando, casal, crise, término e volta), com confiança, ciúme e intimidade.
- **Fofoca e boatos** que passam de garota para garota pelas amizades e se distorcem no caminho.
- **Testemunhas de verdade**: só viu quem estava perto, olhando para você e sem nada no meio (uma conversa, um flerte, mãos dadas, um toque, um beijo, H).
- **Reputação de cada garota** (0–100): sobe com amigas, desce com inimigas; populares e excluídas.
- **Laços em movimento**: histórias de 3 ou 4 dias que duas alunas vivem sozinhas (amigas se afastando, rivais obrigadas a trabalhar juntas, um hobby em comum, uma briga antiga resolvida).
- **Pares no mapa**: duas garotas vêm te chamar juntas, com um motivo.
- **Harém**: duas namoradas que se gostam podem aceitar dividir você, em três conversas.
- **Mágoas e reconciliação**, ciúme pela relação, memória do que sua namorada já decidiu sobre você.
- **Humores e necessidades** (alegre, com fome, entediada, estressada, sozinha, flertando, com sono, chateada).
- **O tempo do dia**, sorteado pelo mês (época das chuvas em junho, tufões em setembro): a chuva muda falas, quem sai ao ar livre e eventos.
- **Professoras e a enfermeira agem como funcionárias**: sentam nas mesas da sala dos professores, trabalham na enfermaria e andam pelos corredores. Falar com uma abre uma conversa própria, pela personalidade e pelo trabalho dela (um aumento, os resultados da turma, corrigir provas a noite toda, uma reclamação). Os alunos fofocam quando você se envolve com uma, e as professoras dão bronca.
- **Heroínas da história e professoras no campus**: o diretor as traz; com turma grande, só as que têm evento no dia vêm com certeza, e a garota que você está cortejando volta no dia seguinte.
- **Destaque da semana**: em turmas grandes, algumas garotas ficam em foco; ninguém fica de fora por mais de 3 semanas.
- **Uma garota cochilando em qualquer lugar** pode ser provocada até acordar; meio dormindo, ela cede mais fácil.

## 4. Vida na faculdade

- **Quadro do clube** com pedidos que as garotas contam a você (um por garota de cada vez, nenhum à noite), com marcadores (estrela, losango, coração) no minimapa, no card dela e no menu. As garotas que sentem sua falta também aparecem no quadro.
- **"Me encontra em..."**: chegue no lugar e no período certos e ela vem mesmo.
- **Atividades e bicos** em pontos reais do campus, com animação (INT, STR, reputação, dinheiro).
- **Uma lojinha na cantina**, presentes do que ela gosta e pedidos de presente do dia; o presente que cumpre o pedido dela vem com moldura rosa.
- **Devoção**: passando de 100 de afeto, a devoção continua subindo e cada nível dá um bônus.
- **Teleporte sem gastar tempo** pelo menu, e caminhada com uma seta dourada até a porta certa.
- **O radar vira mapa**: a planta do andar, e quem está em outro andar aparece apagado com uma setinha.
- **Tela de carregamento** com a foto do mapa de destino (todo mapa tem uma, que vem com o mod), "Going to: ..." e uma dica, no lugar do branco do jogo.
- **Alt+F7** põe os pontos das atividades, o ícone da provocação do cochilo e a foto de carregamento de cada mapa onde você está.

## 5. História e registros

- **Capítulos do namoro**: virar namorado não é o fim; cada namorada passa por capítulos com alguns dias entre eles.
- **Histórias de domingo** em três partes depois do encontro do jogo, com os fundos do jogo.
- **Episódios em destaque** encenados como o modo história, com os fundos do jogo.
- **Eventos do calendário** como cenas longas com escolhas que voltam depois; cada um mantém a sua natureza (Dia do Chocolate, prova, viagem).
- **Projeto do clube** antes do festival, **eleição do grêmio** a cada ciclo de 9 semanas, **ranking do clube** como capítulo da história.
- **Antologia da turma**: os grandes momentos da temporada viram um livro mostrado no último sábado do ciclo.
- **Álbum de conquistas** (F7): registro e lista de afazeres ao mesmo tempo, com etapas ("Familiar Face, 3/4"), segredos com dica e um cartãozinho estilo Steam com som quando uma desbloqueia.
- **Relatório semanal** e um diário no menu.

## 6. Cards masculinos

- Um card salvo como masculino é detectado sozinho, e a pasta **male** aparece ao lado da **female** na escolha da turma. Ele usa o banheiro masculino, perde reputação se entrar nas áreas femininas e flagra as garotas que entram no masculino.
- As falas **sobre ele** dizem he/him/his e boyfriend; **ele mesmo** diz boyfriend ou guy; a voz e a personalidade continuam as mesmas. O card dele diz "Boyfriend".

## 7. Menu e opções

- **F7** abre o menu dentro da prancheta do jogo, com as fontes e sprites dele: Today (estado e pedidos), Calendar, Activities, Album, Details (como tudo funciona) e a loja do campus. À noite, um ícone de prancheta no seu quarto abre o menu para planejar o dia seguinte.
- Na prancheta do jogo (botão do meio), cada card ganha três páginas: Now / With you / Love.
- Toda opção também está no ConfigurationManager (F1), em seções numeradas.

## Os mods irmãos

- **Campus Saves**: um espaço de save por campus, um cofre de versões que guarda dias antigos em pouco espaço, salvar no meio do dia e autosaves inteligentes (Ctrl+F6).
- **School Optimizer**: menos alunas carregadas por período, com prioridade para as importantes (o Campus Life diz quem), guarda de memória, pausa da animação fora da tela, física por distância, limite de tamanho de textura, correções por fora para outros mods (AmazingNewAccessoryLogic, MaterialEditor, UncensorSelector) e guardar os dados de quem está ausente.
- **H Male Wardrobe**: troca a roupa do protagonista no meio do H com uma cópia do menu de roupa da garota (F10).

## Instalação

1. Baixe o zip nas [releases](https://github.com/Jooj000/KK_CampusLife/releases).
2. Solte a pasta `BepInEx` dele na pasta do jogo. O mod inteiro é um arquivo só, `BepInEx\plugins\Joojkk\KK_CampusLife.dll`.
3. Modo história, **F7**.

BepInEx e KKAPI vêm com o HF Patch.

Os dados ficam dentro do save; tirar o mod deixa o save jogável.
