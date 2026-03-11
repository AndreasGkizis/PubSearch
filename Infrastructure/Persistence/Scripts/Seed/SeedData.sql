USE ResearchPublications;
GO

-- ── Authors ───────────────────────────────────────────────────────────────
INSERT INTO Authors (FullName, Email) VALUES
    ('Yann LeCun',            'lecun@fb.com'),
    ('Geoffrey Hinton',       'hinton@google.com'),
    ('Yoshua Bengio',         'bengio@mila.ca'),
    ('Andrej Karpathy',       'karpathy@openai.com'),
    ('Fei-Fei Li',            'feifeili@stanford.edu'),
    ('Ian Goodfellow',        'goodfellow@apple.com'),
    ('Demis Hassabis',        'demis@deepmind.com'),
    ('Ilya Sutskever',        'ilyasu@openai.com'),
    ('Pieter Abbeel',         'abbeel@berkeley.edu'),
    ('Chelsea Finn',          'cfinn@stanford.edu');
GO

-- ── Publications (5 with PdfFileName, 5 without) ─────────────────────────
INSERT INTO Publications (Title, Abstract, Body, Keywords, Year, DOI, CitationCount, PdfFileName) VALUES
(
    'Deep Residual Learning for Image Recognition',
    'We present a residual learning framework to ease the training of networks that are substantially deeper than those used previously. We explicitly reformulate the layers as learning residual functions with reference to the layer inputs, instead of learning unreferenced functions.',
    'Deep neural networks are more difficult to train. We present a residual learning framework to ease the training of networks that are substantially deeper than those used previously. We explicitly reformulate the layers as learning residual functions with reference to the layer inputs. We provide comprehensive empirical evidence showing that these residual networks are easier to optimize, and can gain accuracy from considerably increased depth.',
    'deep learning, residual networks, image recognition, CNN',
    2016,
    '10.1109/CVPR.2016.90',
    120000,
    'paper-01.pdf'
),
(
    'Attention Is All You Need',
    'The dominant sequence transduction models are based on complex recurrent or convolutional neural networks. We propose a new simple network architecture, the Transformer, based solely on attention mechanisms, dispensing with recurrence and convolutions entirely.',
    'Recurrent neural networks, long short-term memory and gated recurrent neural networks in particular, have been firmly established as state of the art approaches in sequence modeling and transduction problems such as language modeling and machine translation. We propose the Transformer, a model architecture eschewing recurrence and instead relying entirely on an attention mechanism to draw global dependencies between input and output.',
    'transformer, attention mechanism, NLP, sequence transduction',
    2017,
    '10.48550/arXiv.1706.03762',
    95000,
    'paper-02.pdf'
),
(
    'Generative Adversarial Networks',
    'We propose a new framework for estimating generative models via an adversarial process, in which we simultaneously train two models: a generative model G that captures the data distribution, and a discriminative model D that estimates the probability that a sample came from the training data rather than G.',
    'Adversarial nets have several advantages and disadvantages relative to other modeling frameworks. The advantages are primarily that Markov chains are never needed, only backpropagation is used to obtain gradients, no inference is needed during learning, and a wide variety of functions can be incorporated into the model.',
    'GAN, generative models, adversarial training, deep learning',
    2014,
    '10.48550/arXiv.1406.2661',
    70000,
    'paper-03.pdf'
),
(
    'BERT: Pre-training of Deep Bidirectional Transformers for Language Understanding',
    'We introduce BERT, a new language representation model designed to pre-train deep bidirectional representations from unlabeled text by jointly conditioning on both left and right context in all layers.',
    'Unlike recent language representation models, BERT is designed to pre-train deep bidirectional representations from unlabeled text by jointly conditioning on both left and right context in all layers. As a result, the pre-trained BERT model can be fine-tuned with just one additional output layer to create state-of-the-art models for a wide range of tasks.',
    'BERT, NLP, pre-training, language model, transformers',
    2019,
    '10.48550/arXiv.1810.04805',
    85000,
    'paper-04.pdf'
),
(
    'Playing Atari with Deep Reinforcement Learning',
    'We present the first deep learning model to successfully learn control policies directly from high-dimensional sensory input using reinforcement learning. The model is a convolutional neural network trained with a variant of Q-learning.',
    'We present the first deep learning model to successfully learn control policies directly from high-dimensional sensory input using reinforcement learning. The model is a convolutional neural network, trained with a variant of Q-learning, whose input is raw pixels and whose output is a value function estimating future rewards.',
    'reinforcement learning, deep Q-network, DQN, Atari, game playing',
    2013,
    '10.48550/arXiv.1312.5602',
    30000,
    'paper-05.pdf'
),
(
    'ImageNet Large Scale Visual Recognition Challenge',
    'The ImageNet Large Scale Visual Recognition Challenge (ILSVRC) is a benchmark in object category classification and detection on hundreds of object categories and millions of images.',
    'We describe the ImageNet Large Scale Visual Recognition Challenge (ILSVRC), a benchmark in object category classification and detection on hundreds of object categories and millions of images. The challenge has been run annually from 2010 to present, attracting participation from more than fifty institutions.',
    'ImageNet, object detection, image classification, benchmark',
    2015,
    '10.1007/s11263-015-0816-y',
    50000,
    NULL
),
(
    'Dropout: A Simple Way to Prevent Neural Networks from Overfitting',
    'Deep neural nets with a large number of parameters are very powerful machine learning systems. However, overfitting is a serious problem in such networks. Dropout is a technique that addresses this problem.',
    'With unlimited computation, the best way to regularize a fixed-sized model is to average the predictions of all possible settings of the parameters, weighting each setting by its posterior probability given the training data. Dropout is a technique where, during training, some neurons are randomly set to zero. This prevents overfitting.',
    'dropout, regularization, neural networks, overfitting',
    2014,
    '10.5555/2627435.2670313',
    42000,
    NULL
),
(
    'Adam: A Method for Stochastic Optimization',
    'We introduce Adam, an algorithm for first-order gradient-based optimization of stochastic objective functions, based on adaptive estimates of lower-order moments.',
    'Adam is computationally efficient, has little memory requirement, is invariant to diagonal rescaling of gradients, and is well suited for problems that are large in terms of data and/or parameters. The method is also appropriate for non-stationary objectives and problems with very noisy and/or sparse gradients.',
    'optimization, Adam, gradient descent, stochastic optimization, deep learning',
    2015,
    '10.48550/arXiv.1412.6980',
    60000,
    NULL
),
(
    'Model-Agnostic Meta-Learning for Fast Adaptation of Deep Networks',
    'We propose an algorithm for meta-learning that is model-agnostic, in the sense that it is compatible with any model trained with gradient descent and applicable to a variety of different learning problems.',
    'The goal of meta-learning is to train a model on a variety of learning tasks, such that it can solve new learning tasks using only a small number of training samples. Our approach trains the model''s initial parameters such that the model has maximal performance on a new task after the parameters have been updated through one or more gradient steps computed with a small amount of data from that new task.',
    'meta-learning, MAML, few-shot learning, gradient descent',
    2017,
    '10.48550/arXiv.1703.03400',
    18000,
    NULL
),
(
    'Neural Architecture Search with Reinforcement Learning',
    'Neural network architectures are typically designed by human experts. In this paper, we use a recurrent network to generate the model descriptions of neural networks and train this recurrent network with reinforcement learning to maximize the expected accuracy of the generated architectures on a validation set.',
    'Our method, starting from scratch, can design a novel network architecture that rivals the best human-invented architecture in terms of test set accuracy. When searching for convolutional architectures, our method, starting from scratch, can design a novel architecture that achieves a test error rate of 3.65 on CIFAR-10.',
    'neural architecture search, NAS, reinforcement learning, AutoML',
    2017,
    '10.48550/arXiv.1611.01578',
    12000,
    NULL
);
GO

-- ── Publication–Author links ──────────────────────────────────────────────
-- Publication 1: LeCun, Hinton
INSERT INTO PublicationAuthors VALUES (1, 1), (1, 2);
-- Publication 2: Sutskever, Hinton
INSERT INTO PublicationAuthors VALUES (2, 8), (2, 2);
-- Publication 3: Goodfellow, Bengio
INSERT INTO PublicationAuthors VALUES (3, 6), (3, 3);
-- Publication 4: Bengio, Hinton
INSERT INTO PublicationAuthors VALUES (4, 3), (4, 2);
-- Publication 5: Sutskever, Hassabis
INSERT INTO PublicationAuthors VALUES (5, 8), (5, 7);
-- Publication 6: Fei-Fei Li, Karpathy
INSERT INTO PublicationAuthors VALUES (6, 5), (6, 4);
-- Publication 7: Hinton
INSERT INTO PublicationAuthors VALUES (7, 2);
-- Publication 8: Bengio
INSERT INTO PublicationAuthors VALUES (8, 3);
-- Publication 9: Finn, Abbeel
INSERT INTO PublicationAuthors VALUES (9, 10), (9, 9);
-- Publication 10: Sutskever
INSERT INTO PublicationAuthors VALUES (10, 8);
GO
