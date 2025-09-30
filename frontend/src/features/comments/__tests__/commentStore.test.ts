import { describe, beforeEach, afterEach, it, expect, vi } from 'vitest';
import { useCommentStore } from '../../../stores/commentStore';
import { commentApi } from '../../../services/commentApi';
import { commentSocket } from '../../../services/commentSocket';
import { CommentStatus, type Comment } from '../../../types/comment';

vi.mock('../../../services/commentApi', () => {
  const stub = {
    getComments: vi.fn(),
    getCommentTree: vi.fn(),
    createComment: vi.fn(),
    updateComment: vi.fn(),
    deleteComment: vi.fn(),
    likeComment: vi.fn(),
    unlikeComment: vi.fn(),
    reportComment: vi.fn(),
    getCommentStats: vi.fn(),
  };
  return { commentApi: stub };
});

vi.mock('../../../services/commentSocket', () => ({
  commentSocket: {
    connect: vi.fn().mockResolvedValue(undefined),
    disconnect: vi.fn().mockResolvedValue(undefined),
    joinPostGroup: vi.fn().mockResolvedValue(true),
    leavePostGroup: vi.fn().mockResolvedValue(true),
    // Returns a no-op unsubscribe function for event listeners
    on: vi.fn().mockReturnValue(() => { /* unsubscribe callback */ }),
    off: vi.fn(),
    getCommentStats: vi.fn(),
    getOnlineUserCount: vi.fn(),
    connectionStatus: { status: 'connected' as const },
    isConnected: true,
  },
}));

vi.mock('../../../services/loggingService', () => ({
  logger: {
    debug: vi.fn(),
    info: vi.fn(),
    warn: vi.fn(),
    error: vi.fn(),
  },
}));

vi.mock('../../../services/errorReporting', () => ({
  errorReporter: {
    captureError: vi.fn(),
  },
}));

const mockedCommentApi = vi.mocked(commentApi);
const mockedCommentSocket = vi.mocked(commentSocket);

const baseComment: Comment = {
  id: 'c1',
  postId: 'p1',
  authorId: 'u1',
  author: {
    id: 'u1',
    username: 'tester',
    displayName: 'Tester',
    role: 'User',
    isVip: false,
  },
  parentId: undefined,
  content: 'Hello world',
  renderedContent: '<p>Hello world</p>',
  status: CommentStatus.Approved,
  depth: 0,
  threadPath: 'c1',
  likeCount: 0,
  replyCount: 0,
  isLiked: false,
  canEdit: true,
  canDelete: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  replies: [],
};

describe('commentStore interactions', () => {
  beforeEach(() => {
    useCommentStore.getState().actions.resetAll();
    mockedCommentApi.likeComment.mockReset().mockResolvedValue(undefined);
    mockedCommentApi.unlikeComment.mockReset().mockResolvedValue(undefined);
    mockedCommentApi.deleteComment.mockReset().mockResolvedValue(undefined);
    mockedCommentSocket.connect.mockClear();
    mockedCommentSocket.joinPostGroup.mockClear();

    useCommentStore.setState(state => {
      state.comments[baseComment.id] = { ...baseComment };
      state.commentsByPost[baseComment.postId] = [baseComment.id];
      state.errors = [];
    });
  });

  afterEach(() => {
    useCommentStore.getState().actions.resetAll();
  });

  it('increments like count and marks comment as liked on success', async () => {
    await useCommentStore.getState().actions.likeComment(baseComment.id);

    const comment = useCommentStore.getState().comments[baseComment.id];
    expect(mockedCommentApi.likeComment).toHaveBeenCalledWith(baseComment.id);
    expect(comment.likeCount).toBe(1);
    expect(comment.isLiked).toBe(true);
  });

  it('records error when like request fails', async () => {
    mockedCommentApi.likeComment.mockRejectedValueOnce(new Error('network error'));

    await useCommentStore.getState().actions.likeComment(baseComment.id);

    const errors = useCommentStore.getState().errors;
    expect(errors).toHaveLength(1);
    expect(errors[0].message).toContain('network error');
  });

  it('removes comment after successful deletion', async () => {
    await useCommentStore.getState().actions.deleteComment(baseComment.id);

    expect(mockedCommentApi.deleteComment).toHaveBeenCalledWith(baseComment.id);
    expect(useCommentStore.getState().comments[baseComment.id]).toBeUndefined();
    expect(useCommentStore.getState().commentsByPost[baseComment.postId]).toEqual([]);
  });
});
