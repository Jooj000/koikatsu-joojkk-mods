# Campus Life — apresentação

**Um mod de vida universitária para o modo história do Koikatsu.** As garotas da faculdade ganham humor, memória, amizades e mágoas próprias; um diretor planeja o dia de cada uma; a conversa vira uma conversa de verdade; e tudo o que você faz com elas vira parte de uma história que a escola inteira comenta.

Feito por **Joojkk** · versão **3.3.0** · BepInEx 5 / KKAPI · Koikatu (HF Patch) · menu **F7** · interface e falas em inglês.

Junto vêm dois mods irmãos: **School Optimizer** (faculdade cheia sem travar) e **H Male Wardrobe** (roupa do protagonista no H, F10).

---

## Em números

| | |
|---|---|
| Código | ~46 mil linhas de C# (.NET 3.5), só prefix/postfix de Harmony, tudo em try/catch |
| Falas | ~2.600 situações escritas à mão + banco procedural de ~6.500 linhas (18 arquivos `.dlg`) |
| Vozes | 4 tons (gentil, enérgica, fria, provocante); 4.464 falas com 4 ou mais versões por tom, sem repetir a mesma em seguida |
| Conquistas | 45 conquistas com estágios + uma escalada para cada heroína da história e cada professora |

---

## 1. A conversa

- **Tela nova de conversa** com cinco categorias em ícones: **Talk**, **Actions**, **Together**, **Romance** e **Spicy**. As opções saem do que ela sabe, do que ela gosta, de onde vocês estão e do que aconteceu no dia, não de uma lista fixa.
- **Conversa procedural**: tópicos com pontuação (o dia dela, vocês dois, você, fofoca, faculdade, papo leve), fatos que ela sabe e de quem soube, opinião e versões distorcidas. Ela lembra o que você contou e puxa o assunto dias depois.
- **A voz dela**: cada fala é escrita nos 4 tons, com a personalidade, o humor e a relação; respostas curtas variadas; o jogo toca um clipe da própria voz gravada dela que combina com a fala.
- **Toques com resposta**: cabeça, bochecha, mãos, peito, além de lugares nossos (bunda, lábios). Ela responde com palavras próprias, conforme a parte, quantas vezes você insistiu, o quanto vocês são próximos e o que o cartão dela aceita. O beijo pode ser sim, sim receoso ou não.
- **Conversa a três**: fale com uma, com a outra ou com as duas. Elas reagem uma à outra, provocam, sentem ciúme, ajudam ou vão embora. Opções que só existem em dupla: perguntar de uma para a outra, uma história constrangedora, bilhete, convite para as duas, elogio na frente da outra, toque escondido.
- **Formas de cena**: conversa com cena, aparte, frase solta, favor, caminhada juntos, momento quieto, triângulo e lembrança (callback).
- **Selos na conversa**: humor e gostos dela em ícones redondos debaixo da barra de H (só depois de descobrir).
- **Nada age com fala na tela**: categorias, opções e toques esperam a caixa de diálogo fechar.

## 2. O diretor (a IA das garotas)

- **O diretor controla 100% da IA**: cada ação de cada garota é escolha dele, com base nas necessidades (companhia, comida, descanso), nos laços e na vida dela. Nada da IA original sobra.
- **Quatro respostas** para tudo o que importa: **sim**, **sim receoso**, **não receoso** e **não**. O diretor lê o máximo de fatores: o que você é para ela, o ritmo dela, as barras, os dias juntos, sua reputação, confiança, humor, H anterior, um beijo recente, outra namorada, um dia perfeito, quantas vezes ela já disse não.
  - O **sim receoso** deixa culpa: na conversa seguinte ela pede que você assuma ou se afasta, e você resolve.
  - O **não receoso** é um quase-sim: ela queria, e diz o que a segurou (personalidade, humor, culpa, sua namorada, cedo demais, já pediu hoje, uma mágoa, reputação, ou só não aqui e agora).
- **Ritmo por personalidade**: garotas reservadas, normais e abertas levam números diferentes de dias para virar amiga, namorar e ir além (tabela abaixo). O H não exige namoro: depende do contexto.
- **Convites, confissão, segurar a mão, beijo, término, traição**: todos decididos pelo diretor. A namorada que descobre outra garota reage pelo humor, raiva, confiança, quantas vezes foi, quem é a outra e se ela viu.
- **Marcadores do cartão** (fácil, pervertida, submissa, séria...): quando o cartão tem todos marcados, o mod lê os que fazem sentido em vez de tornar todas fáceis.
- **Antes do H do jogo** ela diz em voz alta o que o cartão dela recusa (proteção, beijo, porta dos fundos, toques, brinquedos), na voz dela. O H do jogo em si não muda.

### Quantos dias até cada marco

Dias bem jogados (3 conversas por dia, respostas boas), média de duas garotas de cada ritmo, sem humor especial:

| Marco | Reservada | Normal | Aberta |
|---|---|---|---|
| Flerte e elogios funcionam | 2 | 2 | 2 |
| Segurar a mão | 3 | 2 | 2 |
| Amiga | 4 | 3 | 3 |
| Namorada | 5 | 4 | 3 |
| Bunda aceita no 1º toque (namorando / só amigos) | 5 / 6 | 4 / 6 | 3 / 5 |
| Beijo (namorando / só amigos, receoso) | 5 / 6 | 4 / 5 | 3 / 4 |
| H: primeiro "não receoso" (quase sim) | 5 | 4 | pula direto para o sim |
| H namorando | sim no dia 7 | sim no dia 5 | sim receoso no 2, sim no 3 |
| H só amigos | sim receoso no dia 8 | sim receoso no dia 6–7 | sim receoso no 2, sim no 5 |

- Toque na mão e no peito (os do jogo): aceitos no primeiro toque em qualquer dia; o que muda com o estágio (conhecida, amiga, namorada) é a tabela de vozes, e insistir na mesma parte leva a "desconfortável", "para" e raiva.
- **Humor**: flertando adianta tudo cerca de 1 dia (mão, flerte e bunda já no dia 1; a aberta aceita H no dia 1); alegre adianta meio dia; sozinha quase não muda (o quase sim vem um dia antes); chateada atrasa a mão para o dia 5 e o H em 1–2 dias.
- **Ordem das respostas**: nas 60 simulações (6 garotas × 5 humores × namorando ou não) a resposta do H nunca voltou atrás: sempre não → não receoso → sim receoso → sim (pulando degraus, nunca descendo).

## 3. Uma faculdade que vive

- **Amizades, rivalidades e namoros entre elas** (paixão, conversando, casal, crise, término e volta), com confiança, ciúme e intimidade.
- **Fofoca e boatos** que passam de garota em garota pelas amizades e se distorcem no caminho.
- **Testemunhas de verdade**: só viu quem estava perto o bastante para aquilo (conversa, flerte, mão, toque, beijo, H).
- **Reputação de cada garota** (0–100): sobe com amigas, cai com inimigas; populares e excluídas.
- **Laços em movimento**: histórias de 3 ou 4 dias que duas alunas vivem sozinhas (amigas se afastando, rivais obrigadas a trabalhar juntas, um hobby em comum, uma briga antiga resolvida).
- **Duplas no mapa**: duas garotas vêm juntas chamar você, com um motivo.
- **Harém**: duas namoradas que se gostam podem aceitar dividir você, em três conversas.
- **Mágoas e reconciliação**, ciúme por relação, memória do que a namorada já decidiu sobre você.
- **Humores e necessidades** (alegre, com fome, entediada, estressada, sozinha, flertando, com sono, chateada).
- **Chuva**: muda falas, movimento e eventos, com o método mais leve possível.
- **Heroínas da história como alunas** todos os dias, e o diretor escolhe uma cuja história abre hoje.
- **Professoras** que andam pelo campus, com eventos próprios por conversa.
- **Destaque semanal**: em turmas grandes, algumas garotas ficam em foco; ninguém fica de fora mais de 3 semanas.
- **Garota dormindo na enfermaria**: dá para provocá-la acordando; meio dormindo ela cede mais fácil.

## 4. Vida universitária

- **Mural do clube** com pedidos que as garotas contam para você, com marcadores (estrela, losango, coração) no minimapa, no cartão dela e no menu.
- **"Me encontra em..."**: chegue no lugar e período certos e ela vem de verdade.
- **Atividades e bicos** em lugares reais do campus, com as animações do próprio jogo (INT, STR, reputação, dinheiro).
- **Lojinha na cantina**, presentes do que ela gosta e pedidos de presente do dia.
- **Devoção**: depois de 100 de afeto, a devoção continua subindo e cada nível dá um bônus.
- **Teleporte sem gastar tempo** pelo menu e caminhada com seta dourada para a porta certa.
- **Radar vira mapa**: planta do andar, e quem está em outro andar aparece apagado com uma setinha.
- **Tela de transição** cinza com dicas no lugar do branco do jogo.

## 5. História e registros

- **Capítulos do namoro**: virar namorado não é o fim; cada namorada passa por capítulos dias depois um do outro.
- **Histórias de domingo** em três partes depois do encontro do jogo.
- **Episódios em destaque** encenados como o modo história, com os fundos do jogo.
- **Eventos do calendário** em cenas longas com escolhas que se reencontram.
- **Projeto do clube** antes do festival, **eleição do grêmio** a cada ciclo de 9 semanas, **rank do clube** como capítulo da história.
- **Antologia da turma**: os grandes momentos da temporada viram um livro mostrado no último sábado do ciclo.
- **Álbum de conquistas** (F7): registro e lista de tarefas ao mesmo tempo, com estágios ("Familiar Face, 3/4"), segredos com dica e um cartãozinho estilo Steam com som quando uma cai.
- **Relatório da semana** e diário no menu.

## 6. Cards masculinos (novo na 3.2)

- Um card salvo como homem é detectado sozinho, e a pasta **male** aparece ao lado da **female** na escolha da turma. Ele usa o banheiro masculino, perde reputação se entrar nas áreas femininas e pega as garotas que entram no masculino.
- As falas **sobre ele** dizem he/him/his e boyfriend; **ele mesmo** se chama de boyfriend ou guy; a voz e a personalidade continuam as mesmas. O cartão dele diz "Boyfriend".

## 7. Menu e opções

- **F7** abre o menu dentro da prancheta do jogo, com as fontes e sprites dele: Hoje (estado e pedidos), Calendário, Atividades, Álbum, Detalhes (como tudo funciona).
- Na prancheta do jogo (botão do meio), cada cartão ganha três páginas: Agora / Com você / Amor.
- Todas as opções também no ConfigurationManager (F1), agrupadas por assunto.

## Os mods irmãos

- **School Optimizer**: menos alunas carregadas por período com prioridade para as importantes (o Campus Life diz quem), guarda de memória, pausa de animação fora da tela, física por distância, limite de textura, correções de fora para outros mods (AmazingNewAccessoryLogic, MaterialEditor, UncensorSelector) e estacionamento dos dados de quem faltou.
- **H Male Wardrobe**: troca a roupa do protagonista no meio do H com uma cópia do menu de roupa da garota (F10).

## Instalação

1. BepInEx 5.4.23+ e KKAPI 1.45+ / ExtensibleSaveFormat (o HF Patch já tem).
2. `KK_CampusLife.dll` em `BepInEx\plugins\Joojkk\`.
3. Modo história, **F7**.

Os dados ficam dentro do save; tirar o mod deixa o save jogável.
